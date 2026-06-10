import { AuditPanel, Metric } from '../../components/dashboard/DashboardWidgets';
import { statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncState } from '../../providers/civicSyncProvider';

const AdminSyncAuditPage = () => {
  const state = useCivicSyncState();

  return (
    <main className="page-stack proposal-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">Admin Console</p>
          <h2>Sync Audit</h2>
          <p>Monitor outbox, inbox, and peer receipt activity for the selected node.</p>
        </div>
        <span className="status-pill status-pill-success">{state.activeNode.name}</span>
      </section>

      <section className="status-strip ledger-metrics">
        <Metric label="Outbox Events" value={state.outbox.length} />
        <Metric label="Inbox Entries" value={state.inbox.length} />
        <Metric label="Sync Receipts" value={state.receipts.length} />
        <Metric label="Peers" value={state.nodeInfo?.peers?.length ?? 0} />
      </section>

      <div className="proposal-actions-grid admin-sync-grid">
        <AuditPanel title="Outbox Queue" rows={state.outbox.slice(0, 10).map((entry) => [statusText[entry.status] ?? entry.status.toString(), `Retries ${entry.retryCount}`, entry.ledgerEntryId.slice(0, 8)])} />
        <AuditPanel title="Inbox Queue" rows={state.inbox.slice(0, 10).map((entry) => [statusText[entry.status] ?? entry.status.toString(), entry.citizenNationalIdNumber || 'Unknown citizen', entry.ledgerEntryId.slice(0, 8)])} />
        <AuditPanel title="Sync Receipts" rows={state.receipts.slice(0, 10).map((receipt) => [receipt.result.toString(), receipt.targetNodeId.slice(0, 8), new Date(receipt.receivedAtUtc).toLocaleString()])} />
      </div>
    </main>
  );
};

export default AdminSyncAuditPage;
