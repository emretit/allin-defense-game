#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AllinDefense.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Core.Editor;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;

namespace AllinDefense.Editor
{
    public static class ClosedTestServicesEditor
    {
        [MenuItem("ALLIN: Defense/Closed Test/Deploy Test Services")]
        public static async void DeployTestEnvironment()
        {
            try
            {
                if (!Application.dataPath.Contains("AllinDefense-first-region")) throw new InvalidOperationException("Use the isolated closed-test checkout.");
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ":/opt/homebrew/bin:/usr/local/share/dotnet");
                Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD", "Major");
                // Set only this editor's authoring service; do not change shared EditorPrefs.
                var authoring = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Unity.Services.CloudCode.Authoring.Editor.CloudCodeAuthoringServices")).First(t => t != null);
                var settingsType = authoring.Assembly.GetType("Unity.Services.CloudCode.Authoring.Editor.Projects.Settings.ICloudCodeProjectSettings");
                var instance = authoring.BaseType.GetProperty("Instance").GetValue(null);
                var settings = authoring.GetMethod("GetService").MakeGenericMethod(settingsType).Invoke(instance, null);
                settingsType.GetProperty("DotnetPath").SetValue(settings, "/opt/homebrew/bin/dotnet");
                var api = EnvironmentsApi.Instance;
                await api.RefreshAsync();
                if (!api.Environments.Any(e => e.Name == UnityGameServicesController.TestEnvironment))
                {
                    string token = await new AccessTokens().GetServicesGatewayTokenAsync();
                    if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("Unity editor sign-in is required.");
                    string endpoint = $"https://services.unity.com/api/unity/v1/projects/{CloudProjectSettings.projectId}/environments";
                    using var request = new UnityWebRequest(endpoint, "POST");
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{\"name\":\"closed-test\"}"));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type","application/json");
                    request.SetRequestHeader("Authorization","Bearer "+token);
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) throw new InvalidOperationException("Test environment creation failed: HTTP "+request.responseCode);
                    await api.RefreshAsync();
                }
                api.SetActiveEnvironment(UnityGameServicesController.TestEnvironment);
                if (api.ActiveEnvironmentName != "closed-test") throw new InvalidOperationException("Production deployment is prohibited by this helper.");
                var window = Deployments.Instance.DeploymentWindow;
                window.OpenWindow();
                await Task.Delay(1000);
                window = Deployments.Instance.DeploymentWindow;
                var items = window.GetFromFiles(new[] { "Assets/CloudCode/CampaignProgression.ccmr", "Assets/CloudCode/CampaignProgression.ac" });
                if (items.Any(i => i == null)) throw new InvalidOperationException("Deployment resources were not discovered.");
                var result = await window.Deploy(items);
                string evidence = "environment="+api.ActiveEnvironmentName+"\ndeployed="+result.Deployed.Count+"\n"+
                    string.Join("\n",items.Select(i=>i.Path+": "+i.Status.Message+"\n"+i.Status.MessageDetail));
                File.WriteAllText("docs/closed-test/evidence/cloud-deployment.txt",evidence);
                Debug.Log("[CLOSED_TEST_DEPLOYMENT] "+evidence);
            }
            catch(Exception error)
            {
                File.WriteAllText("docs/closed-test/evidence/cloud-deployment.txt","FAILED: "+error.Message);
                Debug.LogError("Closed-test deployment failed: "+error.Message);
            }
        }
    }
}
#endif
