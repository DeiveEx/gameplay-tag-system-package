using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeiveEx.Utilities;
using UnityEngine;

namespace DeiveEx.GameplayTagSystem
{
    public static class GameplayTagDatabase
    {
        #region Fields
        
        private static GameplayTagContainer _tagDatabase = new();

        internal const string TAG_DATABASE_FILE_EXTENSION = ".tags";
        
        #endregion
        
        #region Properties

        public static GameplayTagContainer Database => _tagDatabase;
        internal static string DatabasePath => Path.Combine(Application.streamingAssetsPath, "Tags");

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Load Tags from the given files. If no paths are given, the database will load all files with the ".tags"
        /// extension inside the StreamingAssets folder
        /// </summary>
        /// <param name="tagFilePaths">The paths for the files with the tags</param>
        /// <exception cref="NullReferenceException">Throws if the list of file paths is empty</exception>
        public static void LoadDatabasesFromFiles(IEnumerable<string> tagFilePaths = null)
        {
            _tagDatabase = new ();

            if(tagFilePaths == null)
                tagFilePaths = UtilityServices.FileService.GetFilesWithExtensionRecursive(DatabasePath, TAG_DATABASE_FILE_EXTENSION);
			
            if (!tagFilePaths.Any())
                throw new NullReferenceException("No Tag Database found! Use the Editor Tool under \"Tools > Edit Gameplay Tags\" to create a new Database");

            foreach (var databasePath in tagFilePaths)
            {
                foreach (var tag in File.ReadAllLines(databasePath))
                {
                    if(string.IsNullOrEmpty(tag) || 
                       tag.StartsWith('[') || 
                       tag.StartsWith('#'))
                        continue;

                    _tagDatabase.AddTagInternal(tag);
                }
            }
        }

        public static IEnumerable<string> GetAvailableTabFilesInProject()
        {
            return UtilityServices.FileService.GetFilesWithExtensionRecursive(DatabasePath, TAG_DATABASE_FILE_EXTENSION);
        }
        
        public static string GetDebugInfo()
        {
            return _tagDatabase.GetDebugInfo();
        }

        #endregion
    }
}
