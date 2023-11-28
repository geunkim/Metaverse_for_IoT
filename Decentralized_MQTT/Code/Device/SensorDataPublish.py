import RPi.GPIO as GPIO
import time
import paho.mqtt.client as mqtt
import adafruit_dht
from board import *

# 콜백 함수 정의
def on_connect(client, userdata, flags, rc):
    print("Connected with result code "+str(rc))

client = mqtt.Client()
client.on_connect = on_connect

# 여기에 MQTT 브로커 서버의 주소 입력
client.connect("broker.emqx.io", 1883, 60)

# 비동기 처리를 위해 loop_start() 사용
client.loop_start()


# GPIO 설정
GPIO.setmode(GPIO.BCM)
TRIG = 23
ECHO = 24

GPIO.setup(TRIG,GPIO.OUT)
GPIO.setup(ECHO,GPIO.IN)

while True:
    # 초음파 센서 작동 시작
    GPIO.output(TRIG, False)
    time.sleep(2) # Sensor settles
   
    GPIO.output(TRIG,True)
    time.sleep(0.00001) # Pulse for 10us to start the sensor
    GPIO.output(TRIG,False)

    while GPIO.input(ECHO)==0:
        pulse_start = time.time()

    while GPIO.input(ECHO)==1:
        pulse_end = time.time()
   
    pulse_duration = pulse_end - pulse_start  
    distance= round(pulse_duration *17150 ,2)

    print(distance)
    # "mytopic123" 토픽에 메시지 발행 (센서값)
    client.publish("myTopic1", str(distance))
   
    # 3초 대기
    time.sleep(2)
