import RPi.GPIO as GPIO
import time
import paho.mqtt.client as mqtt
from indy import crypto, wallet, did, pool, ledger
import json
import asyncio
import base64
from datetime import datetime  # 추가된 부분

def on_connect(client, userdata, flags, rc):
    print("Connected with result code " + str(rc))

client = mqtt.Client()
client.on_connect = on_connect

client.connect("192.168.0.23", 1883, 60)

client.loop_start()

GPIO.setmode(GPIO.BCM)
TRIG = 23
ECHO = 24

GPIO.setup(TRIG, GPIO.OUT)
GPIO.setup(ECHO, GPIO.IN)

wallet_config = '{"id": "my_wallet22"}'
wallet_credentials = '{"key": "wallet_key"}'
pool_name = "my_pool43"
seed = "issuer10000000000000000000000000"

pool_genesis_txn_path = "/home/user/Desktop/genesis.txn"

async def connect_to_pool():
    # Pool 연결
    await pool.set_protocol_version(2)
    
    pool_config = json.dumps({"genesis_txn": str(pool_genesis_txn_path)})
    
    await pool.create_pool_ledger_config(pool_name, pool_config)
    
    pool_handle = await pool.open_pool_ledger(pool_name, pool_config)

    return pool_handle

async def close_pool(pool_handle):
    # Pool 연결 닫기
    await pool.close_pool_ledger(pool_handle)

async def create_wallet_and_get_did(pool_handle):
    # Wallet 생성 또는 열기
    try:
        await wallet.create_wallet(wallet_config, wallet_credentials)
    except:
        print("Wallet already created")

    wallet_handle = await wallet.open_wallet(wallet_config, wallet_credentials)

    # DID 생성 또는 가져오기
    try:
        steward_did_json = json.dumps({'seed':seed})
        print(steward_did_json)
        (steward_did, steward_verkey) = await did.create_and_store_my_did(wallet_handle, steward_did_json)
    except:
        print("Steward DID already exists")
        
    print("steward dud : ", steward_did)
        
    try:
        (user_did, user_verkey) = await did.create_and_store_my_did(wallet_handle, "{}")
    except:
        print("User DID already exists")
        
    print("user did : ", user_did)
    print("user publickey : ", user_verkey)
    
    # DID 등록
    nym_request = await ledger.build_nym_request(steward_did, user_did, user_verkey, None, None)
    print(nym_request)
    
    await ledger.sign_and_submit_request(pool_handle, wallet_handle, steward_did, nym_request)
    print("OK!!")

    return wallet_handle, user_did

async def sign_data(wallet_handle, steward_did, data):
    signer_key = await did.key_for_local_did(wallet_handle, steward_did)
    signature = await crypto.crypto_sign(wallet_handle, signer_key, data.encode())
    return base64.b64encode(signature)

async def main():
    # Pool 연결
    pool_handle = await connect_to_pool()

    # Wallet 및 DID 설정
    wallet_handle, did_info = await create_wallet_and_get_did(pool_handle)
    print("wallet handle : ", wallet_handle)
    print("did_info : ", did_info)

    try:
        for _ in range(1):  # 5번 반복
            GPIO.output(TRIG, False)
            time.sleep(2)

            GPIO.output(TRIG, True)
            time.sleep(0.00001)
            GPIO.output(TRIG, False)

            while GPIO.input(ECHO) == 0:
                pulse_start = time.time()

            while GPIO.input(ECHO) == 1:
                pulse_end = time.time()

            pulse_duration = pulse_end - pulse_start
            distance = round(pulse_duration * 17150, 2)
            print(distance)

            # 현재 시간을 얻어옴
            current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

            # 데이터 서명
            signature = await sign_data(wallet_handle, did_info, str(distance))

            # 서명과 데이터를 함께 publish
            payload = {"distance": distance, "signature": signature.decode(), "did": did_info, "timestamp": current_time}
            client.publish("Distance", json.dumps(payload))
            print(json.dumps(payload))

            time.sleep(3)
    finally:
        if wallet_handle is not None:
            # 프로그램 종료 시에 정리 작업 수행
            await wallet.close_wallet(wallet_handle)
        # Pool 연결 닫기
        await close_pool(pool_handle)

try:
    asyncio.get_event_loop().run_until_complete(main())
except KeyboardInterrupt:
    pass
finally:
    # 프로그램 종료 시에 정리 작업 수행
    client.loop_stop()
    GPIO.cleanup()
