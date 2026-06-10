import { Empty } from 'antd';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncState } from '../../providers/civicSyncProvider';

const formatDate = (value: string) => new Date(value).toLocaleString();

const formatFieldValue = (value?: string) => {
  if (!value) {
    return 'No value recorded';
  }

  return value.replace('|', ' / ');
};


const approvalDecisionText: Record<number, string> = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
};

const getRequestStateClass = (status: number) => {
  if (status === 3 || status === 5 || status === 7) {
    return 'status-pill-success';
  }

  if (status === 4 || status === 8 || status === 9) {
    return 'status-pill-danger';
  }

  return 'status-pill-warning';
};

const CitizenLedgerPage = () => {
  const state = useCivicSyncState();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const citizenRequests = selectedCitizen
    ? state.changeRequests.filter((request) => request.citizenId === selectedCitizen.id)
    : state.changeRequests;
  const committedCount = citizenRequests.filter((request) => request.status === 5).length;
  const pendingCount = citizenRequests.filter((request) => request.status === 1 || request.status === 2 || request.status === 3).length;

  return (
    <main className="page-stack proposal-page citizen-ledger-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">Citizen Ledger</p>
          <h2>Audit Ledger</h2>
          <p>Review the citizen record change trail without jumping into a side-card or nested anchor view.</p>
        </div>
        <span className="status-pill status-pill-success">Chain verified</span>
      </section>

      <section className="status-strip ledger-metrics">
        <Metric label="Total Requests" value={citizenRequests.length} />
        <Metric label="Committed Changes" value={committedCount} />
        <Metric label="Awaiting Approval" value={pendingCount} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>

      <section className="panel ledger-history-panel">
        <div className="panel-header">
          <h2>Transaction History</h2>
          {selectedCitizen && <span className="status-pill">SA-CSL-{selectedCitizen.id.slice(0, 4).toUpperCase()}</span>}
        </div>

        {citizenRequests.length === 0 ? (
          <Empty className="empty-text" description="No citizen ledger history found yet." />
        ) : (
          <div className="ledger-timeline">
            {citizenRequests.map((request) => {
              const fieldChange = request.fieldChanges[0];
              return (
                <article className="ledger-entry-card" key={request.id}>
                  <div className="ledger-entry-status">
                    <span className={`status-pill ${getRequestStateClass(request.status)}`}>{statusText[request.status] ?? `Status ${request.status}`}</span>
                    <small>{formatDate(request.createdAtUtc)}</small>
                  </div>

                  <div className="ledger-entry-main">
                    <div>
                      <strong>{fieldChange?.fieldName ?? 'Citizen Record'}</strong>
                      <p>{request.reason}</p>
                    </div>
                    <code>{request.id.slice(0, 8).toUpperCase()}</code>
                  </div>

                  <div className="ledger-value-flow">
                    <div>
                      <span>Previous value</span>
                      <strong>{formatFieldValue(fieldChange?.oldValue)}</strong>
                    </div>
                    <b>?</b>
                    <div>
                      <span>New value</span>
                      <strong>{formatFieldValue(fieldChange?.newValue)}</strong>
                    </div>
                  </div>

                  <div className="ledger-approval-row">
                    {nodes.map((node) => {
                      const approval = request.approvals.find((item) => item.approverDepartmentName === node.departmentCode.toString());
                      return (
                        <span className="department-mini-pill" key={node.departmentCode}>
                          <i className={`status-dot ${node.departmentCode === 2 ? 'orange' : node.departmentCode === 3 ? 'blue' : ''}`} />
                          {node.name}: {approval ? approvalDecisionText[approval.decision] ?? 'Reviewed' : 'Pending'}
                        </span>
                      );
                    })}
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </main>
  );
};

export default CitizenLedgerPage;
