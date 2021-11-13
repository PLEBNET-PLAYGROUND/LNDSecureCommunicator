using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LNDSecureCommunicator.ServiceInterface
{
    public class LNDSecureContext : DbContext
    {
        public DbSet<LNDSecureCommunicatorSetting> LNDSecureCommunicatorSettings { get; set; }
        public DbSet<DecodedMessage> DecodedMessages { get; set; }
        public DbSet<RemoteNode> RemoteNodes { get; set; }

        public string DbPath { get; private set; }

        public LNDSecureContext()
        {
            var path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{System.IO.Path.DirectorySeparatorChar}LNDSecureComms.db";
            DbPath = path;
        }
        public LNDSecureContext(string path)
        {
            DbPath = path;
        }
        //public LNURLContext(IOptions<LNURLSettings> settings)
        //{
        //    var path = settings.Value.DbPath ?? $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{System.IO.Path.DirectorySeparatorChar}LNDSecureComms.db";
        //    DbPath = path;
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }

    public class LNDSecureCommunicatorSetting
    {
        public int Id { get; set; }

        public string? NodePubkey { get; set; }
        public string OnionPublicAddress { get; set; }
        public string KeyType { get; set; }
        public string OnionPrivateKeyBase32 { get; set; }

        public string ClientAuthBase64PrivateKey { get; set; }
        public string ClientAuthBase32PublicKey { get; set; }

        public ulong InvoiceLastIndexOffset { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    [Index(nameof(NodePubkey), IsUnique = false)]
    [Index(nameof(CreatedDate), IsUnique = false)]
    public class DecodedMessage
    {
        public int Id { get; set; }
        public string NodePubkey { get; set; } //fk
        public string Message { get; set; }
        public byte[]? FileData { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [Index(nameof(Disabled), IsUnique = false)]
    public class RemoteNode
    {
        [Key]
        public string NodePubkey { get; set; }
        public bool RemoteNodeACK { get; set; }
        public string SharedBase64PrivateKey { get; set; }

        public string? OnionAddress { get; set; }
        public string? ClientAuthBase32PublicKey { get; set; }

        public bool Disabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
