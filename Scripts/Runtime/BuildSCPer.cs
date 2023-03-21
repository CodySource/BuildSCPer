using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;

namespace CodySource
{
    public class BuildSCPer : ScriptableObject, IPostprocessBuildWithReport
    {

        #region PROPERTIES

        public bool hasInfo => _buildInfo != null;
        public BuildContentsUpload buildContentProcess;
        public List<UploadProcess> additionalUploadProcesses;
        public bool isRunning => _processes > 0;
        public int callbackOrder => 100;

        private BuildInfo _buildInfo = null;
        private int _processes = 0;

        #endregion

        #region PUBLIC METHODS

        public void Run()
        {
            try 
            { 
                string info = File.ReadAllText("Assets/BuildUploader_ActiveBuildInfo.json");
                _buildInfo = JsonConvert.DeserializeObject<BuildInfo>(info);
            }
            catch (System.Exception e) { _buildInfo = null; return; }
            if (_buildInfo == null) return;
            _UploadBuildContents(_buildInfo);
            _RunUploadProcesses(_buildInfo);
        }

        public void OnPostprocessBuild(BuildReport pReport)
        {
            List<string> paths = new List<string>();
            for (int f = 0; f < pReport.files.Length; f++) paths.Add(pReport.files[f].path);
            BuildInfo temp = new BuildInfo()
            {
                startTime = pReport.summary.buildStartedAt,
                filePaths = paths.ToArray(),
                outputPath = pReport.summary.outputPath
            };
            File.WriteAllText("Assets/BuildUploader_ActiveBuildInfo.json", JsonConvert.SerializeObject(temp));
        }

        public void ResetProcesses() => _processes = 0;

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Performs the uploads
        /// </summary>
        private void _RunUploadProcesses(BuildInfo pInfo) => additionalUploadProcesses.ForEach(process => {
            if (process.enabled) process.uploads.ForEach(upload => Process.Start("CMD.exe", $"/C scp -i {process.sshKeyLocation} {UnityEditor.AssetDatabase.GetAssetPath(upload)} {process.remoteLocation}"));
        });

        /// <summary>
        /// Uploads the build contents
        /// </summary>
        private void _UploadBuildContents(BuildInfo pInfo) 
        {
            if (!buildContentProcess.enabled) return;
            //  Add base build files
            List<string> buildFiles = new List<string>(Directory.GetFiles(pInfo.outputPath + "\\Build"));
            List<string> remove = new List<string>();
            for (int i = buildFiles.Count - 1; i > 0; i--) if (System.DateTime.Compare(File.GetCreationTime(buildFiles[i]).ToUniversalTime(), pInfo.startTime) < 0) remove.Add(buildFiles[i]);
            remove.ForEach(f => buildFiles.Remove(f));
            //  Add additional files
            for (int f = 0; f < pInfo.filePaths.Length; f++) buildFiles.Add(pInfo.filePaths[f]);
            buildFiles.ForEach(b =>
            {
                string remote = buildContentProcess.remoteLocation + b.Replace(pInfo.outputPath, "").Replace(Path.GetFileName(b), "");
                if (File.Exists(b))
                {
                    _processes++;
                    Process p = Process.Start("CMD.exe", $"/C scp -i {buildContentProcess.sshKeyLocation} {b} {remote}");
                    p.EnableRaisingEvents = true;
                    p.Exited += (s, e) => _processes--;
                    p.Disposed += (s, e) => _processes--;
                }
            });
        }

        #endregion

        #region PUBLIC STRUCTS

        [System.Serializable]
        public class BuildInfo
        {
            public System.DateTime startTime;
            public string outputPath;
            public string[] filePaths;
        }

        [System.Serializable]
        public struct BuildContentsUpload
        {
            public bool enabled;
            public string sshKeyLocation;
            public string remoteLocation;
            //  Something about including the build folder in the remote location to allow for versioning
        }

        [System.Serializable]
        public struct UploadProcess
        {
            public bool enabled;
            public string sshKeyLocation;
            public List<Object> uploads;
            public string remoteLocation;
        }

        #endregion

    }
}

#else
namespace CodySource { public class PostBuildAutoUploader : ScriptableObject {} }
#endif