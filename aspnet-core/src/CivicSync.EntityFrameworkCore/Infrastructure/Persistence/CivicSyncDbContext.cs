using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Ledger;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.Sync;
using CivicSync.EntityFrameworkCore.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence;

[ConnectionStringName("CivicSyncNode")]
public sealed class CivicSyncDbContext : AbpDbContext<CivicSyncDbContext>
{
    public CivicSyncDbContext(DbContextOptions<CivicSyncDbContext> options)
        : base(options)
    {
    }

    public DbSet<DepartmentNode> DepartmentNodes => Set<DepartmentNode>();
    public DbSet<KnownPeerNode> KnownPeerNodes => Set<KnownPeerNode>();
    public DbSet<DepartmentUser> DepartmentUsers => Set<DepartmentUser>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<CitizenReplica> CitizenReplicas => Set<CitizenReplica>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<FieldChange> FieldChanges => Set<FieldChange>();
    public DbSet<DepartmentApproval> DepartmentApprovals => Set<DepartmentApproval>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<SyncOutboxEvent> SyncOutboxEvents => Set<SyncOutboxEvent>();
    public DbSet<SyncInboxEntry> SyncInboxEntries => Set<SyncInboxEntry>();
    public DbSet<NodeSyncReceipt> NodeSyncReceipts => Set<NodeSyncReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDepartmentNode(modelBuilder);
        ConfigureKnownPeerNode(modelBuilder);
        ConfigureDepartmentUser(modelBuilder);
        ConfigureCitizen(modelBuilder);
        ConfigureCitizenReplica(modelBuilder);
        ConfigureChangeRequest(modelBuilder);
        ConfigureFieldChange(modelBuilder);
        ConfigureDepartmentApproval(modelBuilder);
        ConfigureLedgerEntry(modelBuilder);
        ConfigureSyncOutboxEvent(modelBuilder);
        ConfigureSyncInboxEntry(modelBuilder);
        ConfigureNodeSyncReceipt(modelBuilder);
    }

    private static void ConfigureDepartmentNode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartmentNode>(entity =>
        {
            entity.ToTable("DepartmentNodes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DepartmentCode).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.ApiBaseUrl).HasMaxLength(500).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => item.DepartmentCode).IsUnique();
            entity.HasMany(item => item.KnownPeers)
                .WithOne()
                .HasForeignKey(item => item.DepartmentNodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(item => item.KnownPeers).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureKnownPeerNode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnownPeerNode>(entity =>
        {
            entity.ToTable("KnownPeerNodes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PeerDepartmentCode).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.PeerBaseUrl).HasMaxLength(500).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.PeerDepartmentCode }).IsUnique();
        });
    }

    private static void ConfigureDepartmentUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartmentUser>(entity =>
        {
            entity.ToTable("DepartmentUsers");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FullName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Role).HasMaxLength(100).IsRequired();
            entity.Property(item => item.EmailAddress).HasMaxLength(256).IsRequired();
            entity.Property(item => item.IsActive).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.EmailAddress }).IsUnique();
            entity.HasOne<DepartmentNode>()
                .WithMany()
                .HasForeignKey(item => item.DepartmentNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCitizen(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Citizen>(entity =>
        {
            entity.ToTable("Citizens");
            entity.HasKey(item => item.Id);
            ConfigureEncryptedProperty(entity.Property(item => item.NationalIdNumber), "Citizen.NationalIdNumber", 512);
            ConfigureEncryptedProperty(entity.Property(item => item.DateOfBirth), "Citizen.DateOfBirth", 512);
            ConfigureEncryptedProperty(entity.Property(item => item.PassportNumber), "Citizen.PassportNumber", 512);
            ConfigureEncryptedProperty(entity.Property(item => item.BiometricReference), "Citizen.BiometricReference", 2000);
            ConfigureEncryptedProperty(entity.Property(item => item.RelationshipStatus), "Citizen.RelationshipStatus", 1000);
            ConfigureEncryptedProperty(entity.Property(item => item.TaxNumber), "Citizen.TaxNumber", 512);
            ConfigureEncryptedProperty(entity.Property(item => item.EmploymentHistory), "Citizen.EmploymentHistory", 2000);
            ConfigureEncryptedProperty(entity.Property(item => item.IncomeAndInvestmentProfile), "Citizen.IncomeAndInvestmentProfile", 2000);
            ConfigureEncryptedProperty(entity.Property(item => item.BankingAndAssets), "Citizen.BankingAndAssets", 2000);
            ConfigureEncryptedProperty(entity.Property(item => item.ResidentialAddress), "Citizen.ResidentialAddress", 1000);
            ConfigureEncryptedProperty(entity.Property(item => item.RatesAccount), "Citizen.RatesAccount", 512);
            ConfigureEncryptedProperty(entity.Property(item => item.MunicipalServiceStatus), "Citizen.MunicipalServiceStatus", 512);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.RecordVersion).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.NationalIdNumber }).IsUnique();
            entity.OwnsOne(item => item.FullName, owned =>
            {
                ConfigureEncryptedProperty(owned.Property(item => item.FirstName).HasColumnName("FirstName"), "Citizen.FirstName", 512);
                ConfigureEncryptedProperty(owned.Property(item => item.LastName).HasColumnName("LastName"), "Citizen.LastName", 512);
                owned.Ignore(item => item.DisplayName);
            });
            entity.OwnsOne(item => item.ContactDetails, owned =>
            {
                ConfigureEncryptedProperty(owned.Property(item => item.EmailAddress).HasColumnName("EmailAddress"), "Citizen.EmailAddress", 1000);
                ConfigureEncryptedProperty(owned.Property(item => item.PhoneNumber).HasColumnName("PhoneNumber"), "Citizen.PhoneNumber", 512);
            });
        });
    }

    private static void ConfigureCitizenReplica(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CitizenReplica>(entity =>
        {
            entity.ToTable("CitizenReplicas");
            entity.HasKey(item => item.Id);
            ConfigureEncryptedProperty(entity.Property(item => item.SharedDataJson), "CitizenReplica.SharedDataJson");
            entity.Property(item => item.SyncStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.CitizenId }).IsUnique();
        });
    }

    private static void ConfigureChangeRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeRequest>(entity =>
        {
            entity.ToTable("ChangeRequests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Reason).HasMaxLength(500).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.ExpectedCitizenVersion).IsRequired();
            entity.Property(item => item.CommittedCitizenVersion);
            entity.HasMany(item => item.FieldChanges)
                .WithOne()
                .HasForeignKey(item => item.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(item => item.Approvals)
                .WithOne()
                .HasForeignKey(item => item.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureFieldChange(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldChange>(entity =>
        {
            entity.ToTable("FieldChanges");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FieldName).HasMaxLength(100).IsRequired();
            ConfigureEncryptedProperty(entity.Property(item => item.OldValue), "FieldChange.OldValue", 4000);
            ConfigureEncryptedProperty(entity.Property(item => item.NewValue), "FieldChange.NewValue", 4000);
        });
    }

    private static void ConfigureDepartmentApproval(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartmentApproval>(entity =>
        {
            entity.ToTable("DepartmentApprovals");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Decision).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.ApproverFullName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ApproverRole).HasMaxLength(100).IsRequired();
            entity.Property(item => item.ApproverDepartmentName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Comment).HasMaxLength(500);
            entity.HasIndex(item => new { item.ChangeRequestId, item.ApprovingNodeId }).IsUnique();
        });
    }

    private static void ConfigureLedgerEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.OriginatingNodeId, item.SequenceNumber }).IsUnique();
            entity.OwnsOne(item => item.PayloadProof, owned =>
            {
                owned.Property(item => item.Hash).HasColumnName("PayloadProofHash").HasMaxLength(256).IsRequired();
            });
            entity.OwnsOne(item => item.PreviousProof, owned =>
            {
                owned.Property(item => item.Hash).HasColumnName("PreviousProofHash").HasMaxLength(256).IsRequired();
            });
            entity.OwnsOne(item => item.CurrentProof, owned =>
            {
                owned.Property(item => item.Hash).HasColumnName("CurrentProofHash").HasMaxLength(256).IsRequired();
            });
        });
    }

    private static void ConfigureSyncOutboxEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncOutboxEvent>(entity =>
        {
            entity.ToTable("SyncOutboxEvents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.LedgerEntryId }).IsUnique();
        });
    }

    private static void ConfigureSyncInboxEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncInboxEntry>(entity =>
        {
            entity.ToTable("SyncInboxEntries");
            entity.HasKey(item => item.Id);
            ConfigureEncryptedProperty(entity.Property(item => item.CitizenNationalIdNumber), "SyncInboxEntry.CitizenNationalIdNumber", 512);
            ConfigureEncryptedProperty(entity.Property(item => item.FieldChangesJson), "SyncInboxEntry.FieldChangesJson");
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.LedgerEntryId }).IsUnique();
        });
    }

    private static void ConfigureNodeSyncReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NodeSyncReceipt>(entity =>
        {
            entity.ToTable("NodeSyncReceipts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Result).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.SyncOutboxEventId, item.TargetNodeId }).IsUnique();
        });
    }

    private static void ConfigureEncryptedProperty(
        PropertyBuilder<string> propertyBuilder,
        string purpose,
        int? maxLength = null)
    {
        propertyBuilder
            .HasConversion(CreateEncryptedStringConverter(purpose))
            .HasDefaultValue(string.Empty)
            .IsRequired();

        if (maxLength.HasValue)
        {
            propertyBuilder.HasMaxLength(maxLength.Value);
            return;
        }

        propertyBuilder.HasColumnType("nvarchar(max)");
    }

    private static ValueConverter<string, string> CreateEncryptedStringConverter(string purpose)
    {
        return new ValueConverter<string, string>(
            value => MasterRecordEncryption.Encrypt(value, purpose),
            value => MasterRecordEncryption.Decrypt(value, purpose));
    }
}



