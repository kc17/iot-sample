using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using CommandLine;
public class DirectMethodJob
{
    static JobClient _jobClient;
    DirectMethodOptions _opts = null;
    AppSettings _appsettings = null;
    public DirectMethodJob(DirectMethodOptions opts, AppSettings appsettings){
        _appsettings = appsettings;
        _opts = opts;
        if(_jobClient == null){
            _jobClient = JobClient.CreateFromConnectionString(appsettings.IoTHubOwnerConnectionString);
        }
    }
    public async Task StartMethodJob(string jobId)
    {
        Logger.Info($"Starting Direct Method Job:[{jobId}], calling {_opts.Method} on devices which matches \"{_opts.Query}\"");

        CloudToDeviceMethod directMethod = 
            new CloudToDeviceMethod(_opts.Method, TimeSpan.FromSeconds(_opts.Timeout), 
                TimeSpan.FromSeconds(_opts.Timeout));

        directMethod.SetPayloadJson(JsonConvert.SerializeObject(_opts.Payload)); 

        JobResponse result = await _jobClient.ScheduleDeviceMethodAsync(jobId,
                                    _opts.Query,
                                    directMethod,
                                    DateTime.UtcNow,
                                    (long)TimeSpan.FromMinutes(2).TotalSeconds);
        Logger.Info($"[{jobId}]{result.Status}");
    }

    public async Task<int> RunDirectMethodAsync(){
        var jobId = Guid.NewGuid().ToString();
        Task jobTask = Task.Run(() =>  StartMethodJob(jobId));

        Task monitorTask = Task.Run(() => JobMonitor.MonitorAsync(_appsettings, jobId));
        Task.WaitAll(jobTask, monitorTask);
        return 0;
    } 
}