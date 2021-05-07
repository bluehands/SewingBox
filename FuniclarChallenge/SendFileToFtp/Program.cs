using System;
using System.IO;
using System.Threading.Tasks;
using FtpLib;

//Welcome!
//Try to refactor the SnedFileToFtp program (FtpLib is a 'external' library). 
//Make main return 0 in success and 1 in error case and give useful error information to console in case of invalid inputs or ftp errors. 
//Use the Result type from Funicular.Switch nuget package, introduction is found on github (https://github.com/bluehands/Funicular-Switch). 
//Useful helper methods are Map, Bind, Match to build the pipeline, Validate to perform checks on input and Try to turn exceptions into Result(s).

//Goal is to collect as much error information as possible and to respect 'Single Level of Abstraction' and 'Dou not Repeat Yourself' priciples.
namespace SendFileToFtp
{
    public class Program
    {
        /// <summary>
        /// Sends file to ftp server
        /// </summary>
        /// <param name="pathOfFileToSend"></param>
        /// <param name="targetFolder"></param>
        /// <param name="host"></param>
        /// <param name="user"></param>
        /// <param name="port"></param>
        /// <param name="authentication"></param>
        /// <param name="password"></param>
        /// <param name="base64PrivateKey"></param>
        /// <param name="privateKeyFilePath"></param>
        public static async Task Main(
            string pathOfFileToSend,
            string targetFolder,
            string host, 
            string user, 
            int port = 22, 
            string authentication = nameof(AuthenticationMode.Password), 
            string? password = null, 
            string? base64PrivateKey = null, 
            string? privateKeyFilePath = null)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentException("Host may not be empty");

            if (port <= 0)
                throw new ArgumentException("Invalid port. Port has to be greater or equal to zero");

            Uri? uri;
            try
            {
                uri = new UriBuilder(host).Uri;
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid host uri");
            }

            if (!Enum.TryParse(typeof(AuthenticationMode), authentication, out var authenticationMode))
            {
                throw new ArgumentException(
                    $"Parameter authentication has invalid value. Valid values are: {nameof(AuthenticationMode.Password)}, {nameof(AuthenticationMode.PrivateKey)}");
            }

            FtpCredentials? credentials = null;
            switch ((AuthenticationMode)authenticationMode)
            {
                case AuthenticationMode.Password:
                    if (password == null)
                        throw new ArgumentException("No password provided");
                    if (user == null)
                        throw new ArgumentException("User not provided");
                    credentials = FtpCredentials.Password(user, password);
                    break;
                case AuthenticationMode.PrivateKey:
                    if (user == null)
                        throw new ArgumentException("User not provided");
                    if (base64PrivateKey != null)
                    {
                        var privateKey = Convert.FromBase64String(base64PrivateKey);
                        credentials = FtpCredentials.PrivateKey(user, privateKey);
                    }
                    else if (privateKeyFilePath != null)
                    {
                        try
                        {
                            var privateKey = await File.ReadAllBytesAsync(privateKeyFilePath);
                            credentials = FtpCredentials.PrivateKey(user, privateKey);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException("Failed to read private key file", e);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (string.IsNullOrEmpty(pathOfFileToSend))
            {
                throw new ArgumentException("No upload file path specified");
            }

            if (string.IsNullOrEmpty(targetFolder))
            {
                throw new ArgumentException("No target folder specified");
            }
            if (!targetFolder.StartsWith("/"))
            {
                throw new ArgumentException("Target folder has to be absolute path on ftp server");
            }

            byte[] fileToUpload;
            try
            {
                fileToUpload = await File.ReadAllBytesAsync(pathOfFileToSend);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to file to upload", e);
            }

            var ftpClient = new FtpClient();
            using var connection = await ftpClient.Connect(uri, port, credentials);
            if (connection.IsConnected)
            {
                await ftpClient.UploadFile(connection, fileToUpload, targetFolder);
                Console.WriteLine("File uploaded successfully");
            }
            else
            {
                throw new Exception($"Connect failed: {connection.ConnectError}");
            }
        }
    }

    enum AuthenticationMode
    {
        Password,
        PrivateKey
    }
}
