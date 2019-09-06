using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NLog;
using SmartHouse.Entities;
using SmartHouse.Hub.Model;
using SmartHub.Dal.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHouse.Hub.Hubs
{
    public class SmartHouseHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public SmartHouseHub(IList<TelemetryDynamic> diagram,
            IOptions<UrlDataConfiguration> config,
            IRepository<Device> deviceRepository,
            IRepository<Sensor> sensorRepository,
            IRepository<TelemetryDynamic> sensorDataRepository,
            ConcurrentDictionary<string, HouseSlaveDeviceClient> devices,
            ConcurrentDictionary<string, HouseSlaveSensorClient> sensors)
        {
            DataFromSensor = diagram;
            Config = config.Value;
            DeviceRepository = deviceRepository;
            SensorRepository = sensorRepository;
            SensorDataRepository = sensorDataRepository;
            Devices = devices;
            Sensors = sensors;

        }

        private ConcurrentDictionary<string, HouseSlaveDeviceClient> Devices { get; }

        private ConcurrentDictionary<string, HouseSlaveSensorClient> Sensors { get; }

        private IList<TelemetryDynamic> DataFromSensor { get; }

        private IRepository<Device> DeviceRepository { get; }

        private IRepository<Sensor> SensorRepository { get; }

        public IRepository<TelemetryDynamic> SensorDataRepository { get; set; }

        private UrlDataConfiguration Config { get; }

        public CancellationToken Token { get; set; }

        public override async Task OnConnectedAsync()
        {
            await Clients.Others.SendAsync("Send", $"{Context.ConnectionId} join.");
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            ChangeStateToOffWhenDisconnect();
            await Clients.Others.SendAsync("Send", $"{Context.ConnectionId} left.");
        }

        #region Initialize methods

        public async Task<bool> InitSensor(Sensor newSlave)
        {
            if (newSlave != null)
            {
                var success = Sensors.TryAdd(newSlave.MAC, new HouseSlaveSensorClient { Slave = newSlave, ConnectionId = Context.ConnectionId });
                if (success)
                {
                    _logger.Info($"Sensor with MAC: {newSlave.MAC}  has been added to current list");

                    var containsEntity =
                        await SensorRepository.ContainsEntityWithMAC(newSlave.MAC, CancellationToken.None);

                    if (!containsEntity)
                    {
                        await SensorRepository.Add(newSlave, Token);
                        _logger.Info($"Sensor with MAC: {newSlave.MAC} has been added to repository.");
                    }
                    else
                    {
                        await SensorRepository.Update(newSlave, Token);
                        _logger.Info($"Sensor with MAC: {newSlave.MAC} has been updated.");
                    }
                }
            }

            return false;
        }

        public async Task<bool> InitDevice(Device newSlave)
        {
            if (newSlave != null)
            {
                var success = Devices.TryAdd(newSlave.MAC, new HouseSlaveDeviceClient { Slave = newSlave, ConnectionId = Context.ConnectionId });
                if (success)
                {
                    _logger.Info($"Device with MAC: {newSlave.MAC}  has been added to current list");

                    var containsEntity =
                        await DeviceRepository.ContainsEntityWithMAC(newSlave.MAC, CancellationToken.None);

                    if (!containsEntity)
                    {
                        await DeviceRepository.Add(newSlave, Token);
                        _logger.Info($"Device with MAC: {newSlave.MAC} has been added to repository.");
                    }
                    else
                    {
                        await DeviceRepository.Update(newSlave, Token);
                        _logger.Info($"Device with MAC: {newSlave.MAC} has been updated.");
                    }
                }
            }

            return false;
        }

        #endregion

        #region Methods what change status
        public void StatusChanger(string slaveMac, SlaveStatus slaveStatus)
        {
            var device = Devices.FirstOrDefault(x => x.Key == slaveMac);
            if (device.Key != null)
            {
                device.Value.Slave.Status = slaveStatus;
                Devices.AddOrUpdate(slaveMac, device.Value, (key, oldVal) => device.Value);
                _logger.Info($"Status of device with MAC {slaveMac} has been updated to {slaveStatus}");
            }
            else
            {
                var sensor = Sensors.FirstOrDefault(x => x.Key == slaveMac);

                if (sensor.Key != null)
                {
                    sensor.Value.Slave.Status = slaveStatus;
                    Sensors.AddOrUpdate(slaveMac, sensor.Value, (key, oldVal) => sensor.Value);
                    _logger.Info($"Status of sensor with MAC {slaveMac} has been updated to {slaveStatus}");
                }
                else
                {
                    _logger.Info($"Can't find device or sensor with MAC: {slaveMac}.");
                }
            }
        }

        private void ChangeStateToOffWhenDisconnect()
        {
            var device = Devices.FirstOrDefault(x => x.Value.ConnectionId.Equals(Context.ConnectionId));

            if (device.Value != null)
            {
                Devices.Remove(device.Key, out _);
                _logger.Info($"Device with MAC: {device.Key} disconnected and was deleted from current list. It's status has been changed to \"Off\"");
            }
            else
            {
                var sensor = Sensors.FirstOrDefault(x => x.Value.ConnectionId.Equals(Context.ConnectionId));

                if (sensor.Value != null)
                {
                    Sensors.Remove(sensor.Key, out _);
                    _logger.Info($"Sensor with MAC: {device.Key} disconnected and was deleted from current list. It's status has been changed to \"Off\"");
                }
            }
        }

        #endregion

        #region Turn on/off methods

        public async Task<bool> TurnOn(HouseSlave newSlave)
        {
            var on = newSlave.On;

            if (on != null)
            {
                await Clients.Caller.SendAsync(on.Address);
                _logger.Info("Method \"On\" has been invoked.");
            }

            return false;
        }

        public async Task<bool> TurnOff(HouseSlave newSlave)
        {
            var off = newSlave.Off;

            if (off != null)
            {
                await Clients.Caller.SendAsync(off.Address);
                _logger.Info("Method \"Off\" has been invoked.");
            }

            return false;
        }

        #endregion

        #region Getting data from sensor method

        public async Task ListenDataFromSensor(TelemetryDynamic sensorData)
        {
            sensorData.TimeToSend.ToUniversalTime().ToString("u");
            DataFromSensor.Add(sensorData);
            await SensorDataRepository.Add(sensorData, Token);
            _logger.Info($"Sensor with MAC: {sensorData.Data.MACSensor} has sent " + sensorData.Data.Value);
        }

        #endregion

        #region Methods for WebAPI 

        public IList<Device> ReturnAllDevices()
        {

            _logger.Info("Return all devices to WebAPI.");
            return Devices.Values.Select(x => x.Slave).ToList();
        }

        public IList<Sensor> ReturnAllSensors()
        {
            _logger.Info("Return all sensors to WebAPI.");
            return Sensors.Values.Select(x => x.Slave).ToList();
        }

        public async Task<bool> RunMethod(string slaveMac, HouseSlaveInvoker method)
        {
            _logger.Info($"Get slave with MAC: {slaveMac} from WebAPI. ");

                var connectionId = Devices.Keys.Contains(slaveMac)
                    ? Devices.Where(x => x.Key == slaveMac).Select(x => x.Value.ConnectionId).FirstOrDefault()
                    : Sensors.Where(x => x.Key == slaveMac).Select(x => x.Value.ConnectionId).FirstOrDefault();

                if (connectionId != null)
                {
                    bool needArgs = method.Args != null && method.Args.Count > 0;
                    if (needArgs)
                    {
                        await Clients.Client(connectionId).SendAsync(method.Address, method.Args.Select(a => a.Value));
                        _logger.Info($"Method with parameters in slave with MAC: {slaveMac} has been invoked.");
                    }
                    else
                    {
                        await Clients.Client(connectionId).SendAsync(method.Address);
                        _logger.Info($"Method without parameters in slave with MAC: {slaveMac} has been invoked.");
                    }

                    return true;
                }

                return false;
        }

        public bool UpdateDevice(Device newSlave)
        {
            var deviceClient = Devices.Where(x => x.Key == newSlave.MAC).Select(x => x.Value).FirstOrDefault();

            if (deviceClient != null)
            {
                deviceClient.Slave.Name = newSlave.Name;
                Devices.AddOrUpdate(newSlave.MAC, deviceClient, (key, oldVal) => deviceClient);
                try
                {
                    DeviceRepository.Update(newSlave, Token);
                }
                catch (Exception e)
                {
                    _logger.Error("Problem with updating to Database.");
                    return false;
                }
                _logger.Info($"Device with MAC {newSlave.MAC} was renamed.");
                return true;
            }

            return false;
        }

        public bool UpdateSensor(Sensor newSlave)
        {
            var sensorClient = Sensors.Where(x => x.Key == newSlave.MAC).Select(x => x.Value).FirstOrDefault();

            if (sensorClient != null)
            {
                sensorClient.Slave.Name = newSlave.Name;
                Sensors.AddOrUpdate(newSlave.MAC, sensorClient, (key, oldVal) => sensorClient);
                try
                {
                    SensorRepository.Update(newSlave, Token);
                }
                catch (Exception e)
                {
                    _logger.Error($"Problem with updating to Database.");
                    return false;
                }
                _logger.Info($"Sensor with MAC {newSlave.MAC} was renamed.");
                return true;
            }

            return false;
        }
        #endregion

    }
}