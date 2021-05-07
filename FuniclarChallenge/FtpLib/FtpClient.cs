using System;
using System.Threading.Tasks;

namespace FtpLib
{
    public class FtpClient
    {
        public Task<FtpConnection> Connect(Uri host, int port, FtpCredentials credentials) => Task.FromResult(new FtpConnection(true, null));

        public Task UploadFile(FtpConnection connection, byte[] fileToUpload, string targetFolder) => Task.CompletedTask;
    }

    public class FtpConnection : IDisposable
    {
        public bool IsConnected { get; }
        public string? ConnectError { get; }

        internal FtpConnection(bool isConnected, string? connectError)
        {
            IsConnected = isConnected;
            ConnectError = connectError;
        }

        public void Dispose()
        {
        }
    }

    public abstract class FtpCredentials
    {
        public static FtpCredentials Password(string username, string password) => new Password_(username, password);
        public static FtpCredentials PrivateKey(string username, byte[] privateKey) => new PrivateKey_(username, privateKey);

        public string Username { get; }

        public class Password_ : FtpCredentials
        {
            public string Password { get; }

            public Password_(string username, string password) : base(UnionCases.Password, username) => Password = password;
        }

        public class PrivateKey_ : FtpCredentials
        {
            public byte[] PrivateKey { get; }

            public PrivateKey_(string username, byte[] privateKey) : base(UnionCases.PrivateKey, username) => PrivateKey = privateKey;
        }

        internal enum UnionCases
        {
            Password,
            PrivateKey
        }

        internal UnionCases UnionCase { get; }
        FtpCredentials(UnionCases unionCase, string username)
        {
            UnionCase = unionCase;
            Username = username;
        }

        public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
        bool Equals(FtpCredentials other) => UnionCase == other.UnionCase;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FtpCredentials)obj);
        }

        public override int GetHashCode() => (int)UnionCase;
    }

    public static class FtpCredentialsExtension
    {
        public static T Match<T>(this FtpCredentials ftpCredentials, Func<FtpCredentials.Password_, T> password, Func<FtpCredentials.PrivateKey_, T> privateKey)
        {
            switch (ftpCredentials.UnionCase)
            {
                case FtpCredentials.UnionCases.Password:
                    return password((FtpCredentials.Password_)ftpCredentials);
                case FtpCredentials.UnionCases.PrivateKey:
                    return privateKey((FtpCredentials.PrivateKey_)ftpCredentials);
                default:
                    throw new ArgumentException($"Unknown type derived from FtpCredentials: {ftpCredentials.GetType().Name}");
            }
        }

        public static async Task<T> Match<T>(this FtpCredentials ftpCredentials, Func<FtpCredentials.Password_, Task<T>> password, Func<FtpCredentials.PrivateKey_, Task<T>> privateKey)
        {
            switch (ftpCredentials.UnionCase)
            {
                case FtpCredentials.UnionCases.Password:
                    return await password((FtpCredentials.Password_)ftpCredentials).ConfigureAwait(false);
                case FtpCredentials.UnionCases.PrivateKey:
                    return await privateKey((FtpCredentials.PrivateKey_)ftpCredentials).ConfigureAwait(false);
                default:
                    throw new ArgumentException($"Unknown type derived from FtpCredentials: {ftpCredentials.GetType().Name}");
            }
        }

        public static async Task<T> Match<T>(this Task<FtpCredentials> ftpCredentials, Func<FtpCredentials.Password_, T> password, Func<FtpCredentials.PrivateKey_, T> privateKey) => (await ftpCredentials.ConfigureAwait(false)).Match(password, privateKey);
        public static async Task<T> Match<T>(this Task<FtpCredentials> ftpCredentials, Func<FtpCredentials.Password_, Task<T>> password, Func<FtpCredentials.PrivateKey_, Task<T>> privateKey) => await(await ftpCredentials.ConfigureAwait(false)).Match(password, privateKey).ConfigureAwait(false);
    }
}
