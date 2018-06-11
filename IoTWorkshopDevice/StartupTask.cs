using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;
using Microsoft.Azure.Devices.Client;
using GrovePi;
using GrovePi.Sensors;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using GrovePi.I2CDevices;

namespace IoTWorkshopDevice
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        static ThreadPoolTimer timer;
        static DeviceClient deviceClient;
        const int HEARTBEAT_FREQUENCY = 5;

        ///**********************************************
        //    Placeholder: Peripherals
        //***********************************************/
        IBuzzer buzzer;
        IRgbLcdDisplay lcd;
        IRotaryAngleSensor rotarySensor;
        ITemperatureAndHumiditySensor tempSensor;

        ///**********************************************
        //    Placeholder: IoT Hub Connection Info
        //***********************************************/
        static readonly string iotHubUri = "<IoT Hub URI here>";
        static readonly string deviceKey = "<IoT Hub Device Key here>";
        static readonly string deviceId = "<IoT Hub Device Id here>";

        public async void Run( IBackgroundTaskInstance taskInstance )
        {
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            deferral = taskInstance.GetDeferral();

            ///**********************************************
            //    Placeholder: Find peripherals
            //***********************************************/
            buzzer = DeviceFactory.Build.Buzzer( Pin.DigitalPin2 );
            lcd = DeviceFactory.Build.RgbLcdDisplay();
            tempSensor = DeviceFactory.Build.TemperatureAndHumiditySensor( Pin.AnalogPin1, Model.Dht11 );
            rotarySensor = DeviceFactory.Build.RotaryAngleSensor( Pin.AnalogPin2 );

            ///**********************************************
            //    Placeholder: IoT Hub create connection
            //***********************************************/
            deviceClient = DeviceClient.Create( iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey( deviceId, deviceKey ), TransportType.Amqp );

            ///**********************************************
            //    Placeholder: Threadpool create telemetry timer
            //***********************************************/
            timer = ThreadPoolTimer.CreatePeriodicTimer( Timer_Tick, TimeSpan.FromSeconds( HEARTBEAT_FREQUENCY ) );
        }

        private async void Timer_Tick( ThreadPoolTimer timer )
        {
            ///**********************************************
            //    Placeholder: Get sensor data and update LCD
            //***********************************************/
            buzzer?.ChangeState( SensorStatus.Off );

            var currentTemp = ConvertTemp.ConvertCelsiusToFahrenheit( tempSensor.TemperatureInCelsius() );
            var rgbVal = Convert.ToByte( rotarySensor.SensorValue() / 4 );

            var sensorText = "Temp: " + currentTemp.ToString( "F" ) + "     Now:  " + DateTime.Now.ToString( "H:mm:ss" );
            Debug.WriteLine( "{0} > Sensor text: {1}", DateTime.Now, sensorText );
            lcd.SetText( sensorText ).SetBacklightRgb( 124, rgbVal, 65 );

            ///**********************************************
            //    Placeholder: Send telemetry to the cloud
            //***********************************************/
            await SendDeviceToCloudMessagesAsync( currentTemp, 32 );

            ///**********************************************
            //    Placeholder: Receive messages from the cloud
            //***********************************************/
            await ReceiveCloudToDeviceMessageAsync();
        }

        /**********************************************
        //  Placeholder: Send telemetry to the cloud
        ***********************************************/
        private static async Task SendDeviceToCloudMessagesAsync( double temperature, double humidity )
        {
            // Create telemetry payload
            var telemetryDataPoint = new
            {
                deviceId,
                temperature,
                humidity,
            };

            // Serialize into a message
            string messageString = JsonConvert.SerializeObject( telemetryDataPoint );
            Message message = new Message( Encoding.ASCII.GetBytes( messageString ) );

            // Send
            await deviceClient.SendEventAsync( message );

            Debug.WriteLine( "{0} > Sent message: {1}", DateTime.Now, messageString );
        }

        /**********************************************
        //  Placeholder: Receive messages from the cloud
        ***********************************************/
        private static async Task ReceiveCloudToDeviceMessageAsync()
        {
            // Pull message from C2D queue
            var receivedMessage = await deviceClient.ReceiveAsync();

            if ( receivedMessage != null )
            {
                Debug.WriteLine( "{0} > Received message: {1}", DateTime.Now, Encoding.ASCII.GetString( receivedMessage.GetBytes() ) );

                // Mark message as received in the C2D queue
                await deviceClient.CompleteAsync( receivedMessage );
            }
        }
    }
}
