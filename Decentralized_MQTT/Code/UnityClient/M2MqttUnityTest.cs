/*
The MIT License (MIT)

Copyright (c) 2018 Giovanni Paolo Vigano'

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using M2MqttUnity;
using Unity.VisualScripting;
using Hyperledger.Indy.WalletApi;
using Hyperledger.Indy.DidApi;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System.IO;
using Org.BouncyCastle.Bcpg.OpenPgp;
using UnityEditor.PackageManager;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.LedgerApi;
using System.Threading.Tasks;
using UnityEditor.Callbacks;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Diagnostics;
using UnityEngine.Profiling;

/// <summary>
/// Examples for the M2MQTT library (https://github.com/eclipse/paho.mqtt.m2mqtt),
/// </summary>
namespace M2MqttUnity.Examples
{
    /// <summary>
    /// Script for testing M2MQTT with a Unity UI
    /// </summary>
    public class M2MqttUnityTest : M2MqttUnityClient
    {
        private List<string> eventMessages = new List<string>();
        private bool updateUI = false;
        

        [Tooltip("Set this to true to perform a testing cycle automatically on startup")]
        public bool autoTest = false;
        [Header("User Interface")]
        public InputField consoleInputField;
        public Toggle encryptedToggle;
        public InputField addressInputField;
        public InputField portInputField;
        public InputField messageInputField;
        public Button connectButton;
        public Button disconnectButton;
        public Button testPublishButton;
        public Button clearButton;
        public Button sendButton;
        public Button requestTopicInfoButton;


        [Header("MQTT Settings")]
        [Tooltip("Topic to subscribe to")]
        public List<string> topic;
        public string targetDid;

        private IndyTest indyTest;
        private DateTime sendMessageTime;
        private DateTime receiveMessageTime;

        protected override void Start()
        {
            SetUiMessage("Ready.");
            updateUI = true;
            base.Start();

            indyTest = GetComponent<IndyTest>();
            requestTopicInfoButton.onClick.AddListener(RequestTopicInfo);
        }

        private void FixedUpdate()
        {
            // 현재 시간 가져오기
            string currentTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 현재 프레임에서의 CPU 사용량을 가져오기
            float cpuUsage = Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;

            // 현재 시간과 CPU 사용량 출력
            UnityEngine.Debug.Log("CPU Usage: " + cpuUsage + " MB" + " time: " + currentTime);

            // 현재 프레임에서의 메모리 사용량 가져오기
            long memoryUsage = Profiler.GetMonoUsedSizeLong() / (1024 * 1024); // MB로 변환

            // 현재 시간과 메모리 사용량 출력
            UnityEngine.Debug.Log($"{currentTime} - 메모리 사용량: {memoryUsage} MB" + " time: " + currentTime);
        }

        private void RequestTopicInfo()
        {
            // 클라이언트가 요청 버튼을 눌렀을 때 브로커에게 토픽 정보를 요청하는 메시지를 발행
            string requestMessage = "RequestingTopicInfo";
            string requestTopic = "request_topic_info";

            client.Publish(requestTopic, Encoding.UTF8.GetBytes(requestMessage), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            AddUiMessage("Requested topic information. Waiting for response...");
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            SetUiMessage("Connected to broker on " + brokerAddress + "\n");

            indyTest.StartIndy();

            // 연결 후 requestTopicInfoButton 활성화
            requestTopicInfoButton.interactable = true;

            if (autoTest)
            {
                TestPublish();
            }
        }

        public async void SendMessage2()
        {
            if (messageInputField)
            {
                string message = messageInputField.text;

                //sing message
                //string signedMessage = await indyTest.SignDataAsync(message);

                // is signed message
                /*if(indyTest.VerifySignature(signedMessage, message))
                {
                    Debug.Log("Verify Signature Success");
                }
                else
                {
                    Debug.Log("Verify Signature Fail");
                }*/

                if (message != "")
                {
                    foreach (string i in topic)
                    {
                        client.Publish(i, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                        // 현재 프레임에서의 CPU 사용량을 가져오기
                        float cpuUsage = Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;

                        UnityEngine.Debug.Log("CPU Usage: " + cpuUsage + " MB");

                        // 현재 프로세스의 메모리 사용량 가져오기
                        Process process = Process.GetCurrentProcess();
                        long memoryUsage = process.WorkingSet64;

                        // 현재 메모리 사용량 출력
                        UnityEngine.Debug.Log($"메모리 사용량: {memoryUsage / (1024 * 1024)} MB");

                        AddUiMessage("Message published.");
                    }
                }
            }
        }


        protected override void DecodeMessage(string topic, byte[] message)
        {
            string msg = System.Text.Encoding.UTF8.GetString(message);

            if (topic == "request_topic_info_response")
            {
                // 브로커로부터 토픽 정보 응답을 받았을 때 처리
                ProcessTopicInfoResponse(msg);
            }
            else
            {
                // 다른 토픽에 온 메시지 처리
                StoreMessage(msg);
                Task.Run(async () => await ProcessReceivedMessages(msg));
            }
        }

        private void ProcessTopicInfoResponse(string response)
        {
            // 브로커로부터 받은 토픽 정보를 출력
            AddUiMessage("Received topic information response: " + response);
        }



        [Serializable]
        private struct DeviceData
        {
            public string originData;
            public string signData;
            public string Did;
        }


        private async Task ProcessReceivedMessages(string receivedMessages)
        {

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Deserialize the JSON message
                var messageObject = JsonConvert.DeserializeObject<DeviceData>(receivedMessages);

                // Extract the DID from the received message
                string receivedDid = messageObject.Did;

                // Query the pool to get the public key associated with the received DID
                string publicKey = await GetPublicKeyFromPoolAsync(receivedDid);

                if (publicKey != null)
                {
                    // using the public key, verify the signature of the received message
                    bool isSignatureValid = await indyTest.VerifySignature(messageObject.signData, messageObject.originData, publicKey);

                    // 서명 검증 종료 시간 기록
                    stopwatch.Stop();

                    if (isSignatureValid)
                    {
                        UnityEngine.Debug.Log("Signature verification successful.");
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Signature verification failed.");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("Failed to retrieve public key for the received DID from the pool.");
                }

                // 측정된 시간 출력
                TimeSpan elapsed = stopwatch.Elapsed;
                UnityEngine.Debug.Log($"서명 검증 시간: {elapsed.TotalSeconds} 초");
            }

            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error processing received message: {ex.Message}");
            }

        }

        private async Task<string> GetPublicKeyFromPoolAsync(string did)
        {
            try
            {
                // Set DID metadata (empty JSON)
                var didResult = Did.SetDidMetadataAsync(indyTest.wallet_handle, did, "{}");

                // Get the public key from the pool
                var keyForDidResult = await Did.KeyForDidAsync(indyTest.pool_handle, indyTest.wallet_handle, did);

                UnityEngine.Debug.Log("Device did: " + did);
                UnityEngine.Debug.Log("publicKey: " + keyForDidResult);

                return keyForDidResult;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error getting public key from the pool: {ex.Message}");
                return null;
            }
        }






        /// <summary>
        /// ////////////////////////////////////////////////////////////////////
        /// </summary>
        public override void Disconnect()
        {
            indyTest.StopIndy();
            base.Disconnect();
        }

        public void TestPublish()
        {
            foreach (string i in topic)
            {
                client.Publish(i, System.Text.Encoding.UTF8.GetBytes("Test message"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                UnityEngine.Debug.Log("Test message published");
                AddUiMessage("Test message published.");
            }
        }

        public void SetBrokerAddress(string brokerAddress)
        {
            if (addressInputField && !updateUI)
            {
                this.brokerAddress = brokerAddress;
            }
        }

        public void SetBrokerPort(string brokerPort)
        {
            if (portInputField && !updateUI)
            {
                int.TryParse(brokerPort, out this.brokerPort);
            }
        }

        public void SetEncrypted(bool isEncrypted)
        {
            this.isEncrypted = isEncrypted;
            UnityEngine.Debug.Log("isEncrypted: " + isEncrypted);
        }


        public void SetUiMessage(string msg)
        {
            if (consoleInputField != null)
            {
                consoleInputField.text = msg;
                updateUI = true;
            }
        }

        public void AddUiMessage(string msg)
        {
            if (consoleInputField != null)
            {
                consoleInputField.text += msg + "\n";
                updateUI = true;
            }
        }

        protected override void OnConnecting()
        {
            base.OnConnecting();
            SetUiMessage("Connecting to broker on " + brokerAddress + ":" + brokerPort.ToString() + "...\n");
        }

        protected override void SubscribeTopics()
        {
            foreach (string i in topic)
            {
                client.Subscribe(new string[] { i }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            //client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        protected override void UnsubscribeTopics()
        {
            foreach (string i in topic)
            {
                client.Unsubscribe(new string[] { i });
            }
            //client.Unsubscribe(new string[] { topic });
        }

        protected override void OnConnectionFailed(string errorMessage)
        {
            AddUiMessage("CONNECTION FAILED! " + errorMessage);
        }

        protected override void OnDisconnected()
        {
            AddUiMessage("Disconnected.");
        }

        protected override void OnConnectionLost()
        {
            AddUiMessage("CONNECTION LOST!");
        }

        private void UpdateUI()
        {
            if (client == null)
            {
                if (connectButton != null)
                {
                    connectButton.interactable = true;
                    disconnectButton.interactable = false;
                    testPublishButton.interactable = false;
                }
            }
            else
            {
                if (testPublishButton != null)
                {
                    testPublishButton.interactable = client.IsConnected;
                }
                if (disconnectButton != null)
                {
                    disconnectButton.interactable = client.IsConnected;
                }
                if (connectButton != null)
                {
                    connectButton.interactable = !client.IsConnected;
                }
            }
            if (addressInputField != null && connectButton != null)
            {
                addressInputField.interactable = connectButton.interactable;
                addressInputField.text = brokerAddress;
            }
            if (portInputField != null && connectButton != null)
            {
                portInputField.interactable = connectButton.interactable;
                portInputField.text = brokerPort.ToString();
            }
            if (encryptedToggle != null && connectButton != null)
            {
                encryptedToggle.interactable = connectButton.interactable;
                encryptedToggle.isOn = isEncrypted;
            }
            if (clearButton != null && connectButton != null)
            {
                clearButton.interactable = connectButton.interactable;
            }
            updateUI = false;
        }


        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg)
        {
            AddUiMessage("Received: " + msg);
        }

        protected override void Update()
        {
            base.Update(); // call ProcessMqttEvents()

            if (eventMessages.Count > 0)
            {
                foreach (string msg in eventMessages)
                {
                    ProcessMessage(msg);
                }
                eventMessages.Clear();
            }
            if (updateUI)
            {
                UpdateUI();
            }
        }



        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnValidate()
        {
            if (autoTest)
            {
                autoConnect = true;
            }
        }

        public void PublishEncryptedMessage(string topic, string message)
        {
            // Replace 'publicKey' with the actual public key of the receiver.
            
            /*
             * client.Publish(topic,
                           Encoding.UTF8.GetBytes(EncryptString(message, PublicKey)),
                           MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
                           false);
            */
        }
    }
}

