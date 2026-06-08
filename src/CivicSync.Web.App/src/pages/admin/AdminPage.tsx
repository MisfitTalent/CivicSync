import { AuditPanel, Info, Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import { useAuthState } from '../../providers/authProvider';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

const AdminPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const { currentUser } = useAuthState();
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  return (
    <main className="page-stack">
      <section className="page-intro">
        <div>
          <p className="eyebrow">Admin workspace</p>
          <h2>System monitoring</h2>
          <p>Admin users monitor nodes, queue sizes, ledger activity, and peer sync outcomes. They do not submit citizen profile changes.</p>
        </div>
        <div className="department-metrics">
          <Metric label="Citizens" value={state.citizens.length} />
          <Metric label="Outbox" value={state.outbox.length} />
          <Metric label="Receipts" value={state.receipts.length} />
        </div>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <div className="department-grid">
        <section className="panel span-2">
          <PanelHeader title="Operational Context" actionLabel="Refresh" onAction={actions.refreshAll} />
          <div className="info-grid">
            <Info label="Signed In" value={currentUser?.displayName ?? 'Unknown'} />
            <Info label="Active Node" value={state.activeNode.name} />
            <Info label="API Base URL" value={state.nodeInfo?.apiBaseUrl ?? state.activeNode.baseUrl} />
            <Info label="Peer Count" value={state.nodeInfo?.peers?.length ?? 0} />
          </div>
        </section>

        <AuditPanel title="Ledger" rows={state.ledger.slice(0, 8).map((entry) => [`#${entry.sequenceNumber}`, entry.currentProofHash.slice(0, 16), new Date(entry.createdAtUtc).toLocaleString()])} />
        <AuditPanel title="Outbox Queue" rows={state.outbox.slice(0, 8).map((entry) => [entry.status.toString(), `Retries ${entry.retryCount}`, entry.ledgerEntryId.slice(0, 8)])} />
        <AuditPanel title="Inbox Queue" rows={state.inbox.slice(0, 8).map((entry) => [entry.status.toString(), entry.citizenNationalIdNumber || 'Unknown citizen', entry.ledgerEntryId.slice(0, 8)])} />
        <AuditPanel title="Sync Receipts" rows={state.receipts.slice(0, 8).map((receipt) => [receipt.result.toString(), receipt.targetNodeId.slice(0, 8), new Date(receipt.receivedAtUtc).toLocaleString()])} />
      </div>
    </main>
  );
};

export default AdminPage;
