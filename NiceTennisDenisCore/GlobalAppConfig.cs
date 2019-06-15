using System.IO;
using Microsoft.Extensions.Configuration;

namespace NiceTennisDenisCore
{
    /// <summary>
    /// Application configuration.
    /// </summary>
    internal static class GlobalAppConfig
    {
        /// <summary>
        /// Wta context y/n.
        /// </summary>
        internal static bool IsWtaContext { get; set; }

        private static IConfiguration _inner;

        private const string MAIN_APP_KEY = "application";

        /// <summary>
        /// Initialize the configuration.
        /// </summary>
        /// <param name="inner">The inner <see cref="IConfiguration"/>.</param>
        internal static void Initialize(IConfiguration inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Gets a <seealso cref="string"/> config value.
        /// </summary>
        /// <param name="key">The config key.</param>
        /// <returns>The string value.</returns>
        internal static string GetString(AppKey key)
        {
            return _inner[string.Format("{0}:{1}", MAIN_APP_KEY, key)];
        }

        /// <summary>
        /// Gets a <seealso cref="bool"/> config value.
        /// </summary>
        /// <param name="key">The config key.</param>
        /// <returns>The boolean value.</returns>
        internal static bool GetBool(AppKey key)
        {
            return _inner[string.Format("{0}:{1}", MAIN_APP_KEY, key)].ToLowerInvariant().Equals(bool.TrueString.ToLowerInvariant());
        }

        /// <summary>
        /// Gets a <seealso cref="int"/> config value.
        /// </summary>
        /// <param name="key">The config key.</param>
        /// <returns>The int value.</returns>
        internal static int GetInt32(AppKey key)
        {
            int.TryParse(_inner[string.Format("{0}:{1}", MAIN_APP_KEY, key)], out int intValue);
            return intValue;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        internal static string GetConnectionString()
        {
            return string.Format(GetString(AppKey.ConnectionStringPattern),
                GetString(AppKey.SQL_instance),
                GetString(IsWtaContext ? AppKey.SQL_db_wta : AppKey.SQL_db_atp),
                GetString(AppKey.SQL_uid),
                GetString(AppKey.SQL_pwd));
        }

        /// <summary>
        /// Gets the base directory for profile pictures.
        /// </summary>
        /// <returns>Profile pictures base directory.</returns>
        internal static string GetProfilePictureBaseDirectory()
        {
            return Path.Combine(GetString(AppKey.DatasDirectory), "profiles", IsWtaContext ? "wta" : "atp");
        }
    }
}
