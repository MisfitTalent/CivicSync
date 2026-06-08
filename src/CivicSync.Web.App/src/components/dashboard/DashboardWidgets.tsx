import { Button, Card, Empty, Input } from 'antd';

export const Metric = ({ label, value }: { label: string; value: number }) => (
  <Card className="metric-card">
    <span>{label}</span>
    <strong>{value}</strong>
  </Card>
);

export const Info = ({ label, value }: { label: string; value: string | number }) => (
  <div className="info-item">
    <span>{label}</span>
    <strong>{value}</strong>
  </div>
);

export const TextInput = ({ label, value, onChange, type = 'text', required = false }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean }) => (
  <label>
    <span>{label}</span>
    <Input type={type} value={value} onChange={(event) => onChange(event.target.value)} required={required} />
  </label>
);

export const PanelHeader = ({ title, actionLabel, onAction }: { title: string; actionLabel: string; onAction: () => void }) => (
  <div className="panel-header">
    <h2>{title}</h2>
    <Button onClick={onAction}>{actionLabel}</Button>
  </div>
);

export const AuditPanel = ({ title, rows }: { title: string; rows: string[][] }) => (
  <Card className="panel">
    <h2>{title}</h2>
    <div className="audit-list">
      {rows.length === 0 ? <Empty className="empty-text" description="No records yet." /> : rows.map((row, index) => (
        <div className="audit-row" key={`${title}-${index}`}>
          <strong>{row[0]}</strong>
          <span>{row[1]}</span>
          <small>{row[2]}</small>
        </div>
      ))}
    </div>
  </Card>
);
