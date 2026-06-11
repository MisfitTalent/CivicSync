import { AuditPanel, Metric } from '../../components/dashboard/DashboardWidgets';
import { statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncState } from '../../providers/civicSyncProvider';

const receiptResultText: Record<number, string> = {
  1: 'Applied by peer',
  2: 'Queued for review',
  3: 'Stored for matching',
  4: 'Security check failed',
  5: 'Delivery failed',
};

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
        <AuditPanel title="Outbox Queue" rows={state.outbox.slice(0, 10).map((entry) => [statusText[entry.status] ?? 'Queued', entry.retryCount > 0 ? 'Retry scheduled' : 'Ready for delivery', 'Peer departments'])} />
        <AuditPanel title="Inbox Queue" rows={state.inbox.slice(0, 10).map((entry) => [statusText[entry.status] ?? 'Received', entry.citizenNationalIdNumber || 'Citizen pending match', entry.appliedAtUtc ? 'Applied' : 'Awaiting review'])} />
        <AuditPanel title="Sync Receipts" rows={state.receipts.slice(0, 10).map((receipt) => [receiptResultText[receipt.result] ?? 'Delivery recorded', 'Peer department', new Date(receipt.receivedAtUtc).toLocaleString()])} />
      </div>
    </main>
  );
};

export default AdminSyncAuditPage;
