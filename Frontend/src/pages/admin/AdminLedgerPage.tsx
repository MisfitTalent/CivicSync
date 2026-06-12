import { Empty } from 'antd';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { useCivicSyncState } from '../../providers/civicSyncProvider';

const AdminLedgerPage = () => {
  const state = useCivicSyncState();

  return (
    <main className="page-stack proposal-page citizen-ledger-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">Admin Console</p>
          <h2>Ledger</h2>
          <p>Inspect ledger entries for the currently selected department node.</p>
        </div>
        <span className="status-pill status-pill-success">{state.activeNode.name}</span>
      </section>

      <section className="status-strip ledger-metrics">
        <Metric label="Ledger Entries" value={state.ledger.length} />
        <Metric label="Outbox Events" value={state.outbox.length} />
        <Metric label="Inbox Entries" value={state.inbox.length} />
        <Metric label="Sync Receipts" value={state.receipts.length} />
      </section>

      <section className="panel ledger-history-panel">
        <div className="panel-header">
          <h2>Ledger Entries</h2>
          <span className="status-pill">{state.activeNode.name}</span>
        </div>

        {state.ledger.length === 0 ? (
          <Empty className="empty-text" description="No ledger entries recorded on this node yet." />
        ) : (
          <div className="ledger-timeline">
            {state.ledger.slice(0, 16).map((entry) => (
              <article className="ledger-entry-card" key={entry.id}>
                <div className="ledger-entry-status">
                  <span className="status-pill status-pill-success">Entry {entry.sequenceNumber}</span>
                  <small>{new Date(entry.createdAtUtc).toLocaleString()}</small>
                </div>
                <div className="ledger-entry-main">
                  <div>
                    <strong>Verified citizen record update</strong>
                    <p>Tamper-evident audit entry retained for this department.</p>
                  </div>
                  <span className="status-pill">Recorded</span>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  );
};

export default AdminLedgerPage;
