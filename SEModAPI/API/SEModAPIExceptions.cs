﻿/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/*
 * This file is intended to contains all exception classes of the API
 * They must inheritate from IExceptionState and must retain the form
 * of GameInstallationExceptionInfo for standardisation.
 * 
 * */
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using SEModAPI.Support;

namespace SEModAPI.API
{
    #region "GameInstallationException"
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Enum to define state of Exceptions used into GameInstallation.cs
    /// </summary>
    public enum GameInstallationInfoExceptionState
    {
        Invalid,
        GamePathNotFound,
        GameNotRegistered,
        SteamPathNotFound,
        SteamNotRegistered,
        BrokenGameDirectory,
        ConfigFileCorrupted,
        ConfigFileMissing,
        ConfigFileEmpty
    };
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// ExceptionInfo used into GameInstallation.cs and ConfigFileSerializer.cs
    /// </summary>
    public class GameInstallationInfoException : IExceptionState
    {
        public GameInstallationInfoException(GameInstallationInfoExceptionState state) : base(state) { }

        public new string[] StateRepresentation =
        {
            "Invalid",
            "GamePathNotFound",
            "GameNotRegistered",
            "SteamPathNotFound",
            "SteamNotRegistered",
            "BrokenGameDirectory",
            "ConfigFileCorrupted",
            "ConfigFileMissing",
            "ConfigFileEmpty"
        };
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion
}