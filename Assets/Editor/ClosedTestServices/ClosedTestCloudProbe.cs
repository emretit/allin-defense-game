#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenderChronicles.Core;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core.Editor.Environments;
namespace DefenderChronicles.Editor
{
    public static class ClosedTestCloudProbe
    {
        [UnityEditor.MenuItem("Defender Chronicles/Closed Test/Probe Test Services")]
        public static async void Run()
        {
            try
            {
                if(EnvironmentsApi.Instance.ActiveEnvironmentName!="closed-test")throw new InvalidOperationException("Select the closed-test environment first.");
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName("closed-test").SetProfile("ctprobe"+Guid.NewGuid().ToString("N").Substring(0,8)));
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                string owner=AuthenticationService.Instance.PlayerId;
                var client=CloudCodeService.Instance;
                await client.CallModuleEndpointAsync<CampaignSyncResult>("CampaignProgression","GetState",new Dictionary<string,object>());
                var claim=new CampaignCompletionClaim{claimId=Guid.NewGuid().ToString("N"),ownerPlayerId=owner,sequence=1,level=1,levelId="level-01",remainingLives=20,stars=1};
                var zero=new CampaignCompletionClaim{claimId=Guid.NewGuid().ToString("N"),ownerPlayerId=owner,sequence=2,level=1,levelId="level-01",remainingLives=0,stars=3};
                var wrong=new CampaignCompletionClaim{claimId=Guid.NewGuid().ToString("N"),ownerPlayerId="different-owner",sequence=3,level=1,levelId="level-01",remainingLives=20,stars=3};
                var result=await client.CallModuleEndpointAsync<CampaignSyncResult>("CampaignProgression","SubmitCompletionBatch",new Dictionary<string,object>{{"claims",new[]{claim,zero,wrong}}});
                var repeat=await client.CallModuleEndpointAsync<CampaignSyncResult>("CampaignProgression","SubmitCompletionBatch",new Dictionary<string,object>{{"claims",new[]{claim}}});
                if(result.state.levels.Single().stars!=3||!result.decisions.Any(d=>d.reason=="invalid_lives")||!result.decisions.Any(d=>d.reason=="owner_mismatch")||repeat.decisions.Single().awardedValor!=0||repeat.state.valor!=result.state.valor)
                    throw new InvalidOperationException("Cloud acceptance assertions failed.");
                File.WriteAllText("docs/closed-test/evidence/cloud-integration.json",JsonUtility.ToJson(result,true));
                Debug.Log("[CLOSED_TEST_CLOUD_PASS] environment=closed-test canonical_stars=3 zero_lives=rejected wrong_owner=rejected duplicate_reward=0");
            }
            catch(Exception error)
            {
                File.WriteAllText("docs/closed-test/evidence/cloud-integration-error.txt",error.Message);
                Debug.LogError("Closed-test cloud probe failed: "+error.Message);
            }
        }
    }
}
#endif
