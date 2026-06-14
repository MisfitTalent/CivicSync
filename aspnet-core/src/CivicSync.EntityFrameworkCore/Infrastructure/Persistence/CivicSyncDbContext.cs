using CivicSync.Core.Domain.Auth;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Ledger;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();
    public DbSet<PasskeyChallenge> PasskeyChallenges => Set<PasskeyChallenge>();
    public DbSet<KnownPeerNode> KnownPeerNodes => Set<KnownPeerNode>();
    public DbSet<DepartmentUser> DepartmentUsers => Set<DepartmentUser>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<CitizenReplica> CitizenReplicas => Set<CitizenReplica>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<FieldChange> FieldChanges => Set<FieldChange>();
    public DbSet<ChangeRequestEvidence> ChangeRequestEvidenceFiles => Set<ChangeRequestEvidence>();
    public DbSet<DepartmentApproval> DepartmentApprovals => Set<DepartmentApproval>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<SyncOutboxEvent> SyncOutboxEvents => Set<SyncOutboxEvent>();
    public DbSet<SyncInboxEntry> SyncInboxEntries => Set<SyncInboxEntry>();
    public DbSet<NodeSyncReceipt> NodeSyncReceipts => Set<NodeSyncReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDepartmentNode(modelBuilder);
        ConfigurePasskeyCredential(modelBuilder);
        ConfigurePasskeyChallenge(modelBuilder);
        ConfigureKnownPeerNode(modelBuilder);
        ConfigureDepartmentUser(modelBuilder);
        ConfigureCitizen(modelBuilder);
        ConfigureCitizenReplica(modelBuilder);
        ConfigureChangeRequest(modelBuilder);
        ConfigureFieldChange(modelBuilder);
        ConfigureChangeRequestEvidence(modelBuilder);
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

    private static void ConfigurePasskeyCredential(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasskeyCredential>(entity =>
        {
            entity.ToTable("PasskeyCredentials");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EmailAddress).HasMaxLength(256).IsRequired();
            entity.Property(item => item.CredentialId).HasMaxLength(500).IsRequired();
            entity.Property(item => item.PublicKey).IsRequired();
            entity.Property(item => item.PublicKeyAlgorithm).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.SignCount).IsRequired();
            entity.Property(item => item.LastUsedAtUtc);
            entity.HasIndex(item => item.CredentialId).IsUnique();
            entity.HasIndex(item => item.EmailAddress);
        });
    }

    private static void ConfigurePasskeyChallenge(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasskeyChallenge>(entity =>
        {
            entity.ToTable("PasskeyChallenges");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EmailAddress).HasMaxLength(256).IsRequired();
            entity.Property(item => item.Challenge).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Purpose).HasMaxLength(40).IsRequired();
            entity.Property(item => item.ExpiresAtUtc).IsRequired();
            entity.Property(item => item.UsedAtUtc);
            entity.HasIndex(item => new { item.EmailAddress, item.Purpose, item.Challenge });
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
            entity.Property(item => item.NationalIdNumber).HasMaxLength(30).IsRequired();
            entity.Property(item => item.DateOfBirth).HasMaxLength(60).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.PassportNumber).HasMaxLength(30).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.BiometricReference).HasMaxLength(1500).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.RelationshipStatus).HasMaxLength(200).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.TaxNumber).HasMaxLength(30).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.EmploymentHistory).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.IncomeAndInvestmentProfile).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.BankingAndAssets).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.ResidentialAddress).HasMaxLength(300).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.RatesAccount).HasMaxLength(50).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.MunicipalServiceStatus).HasMaxLength(100).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.RecordVersion).IsRequired();
            entity.HasIndex(item => new { item.DepartmentNodeId, item.NationalIdNumber }).IsUnique();
            entity.OwnsOne(item => item.FullName, owned =>
            {
                owned.Property(item => item.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
                owned.Property(item => item.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
                owned.Ignore(item => item.DisplayName);
            });
            entity.OwnsOne(item => item.ContactDetails, owned =>
            {
                owned.Property(item => item.EmailAddress).HasColumnName("EmailAddress").HasMaxLength(256).IsRequired();
                owned.Property(item => item.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(30).IsRequired();
            });
        });
    }

    private static void ConfigureCitizenReplica(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CitizenReplica>(entity =>
        {
            entity.ToTable("CitizenReplicas");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SharedDataJson).IsRequired();
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
            entity.HasMany(item => item.EvidenceFiles)
                .WithOne()
                .HasForeignKey(item => item.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(item => item.Approvals)
                .WithOne()
                .HasForeignKey(item => item.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureChangeRequestEvidence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeRequestEvidence>(entity =>
        {
            entity.ToTable("ChangeRequestEvidenceFiles");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.SizeBytes).IsRequired();
            entity.Property(item => item.ContentHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Content).IsRequired();
            entity.HasIndex(item => item.ChangeRequestId);
        });
    }

    private static void ConfigureFieldChange(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldChange>(entity =>
        {
            entity.ToTable("FieldChanges");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FieldName).HasMaxLength(100).IsRequired();
            entity.Property(item => item.OldValue).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.NewValue).HasMaxLength(1000).IsRequired();
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
            entity.Property(item => item.CitizenNationalIdNumber).HasMaxLength(30).IsRequired();
            entity.Property(item => item.FieldChangesJson).IsRequired();
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
}



