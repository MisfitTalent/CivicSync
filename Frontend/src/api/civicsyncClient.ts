import { createCivicSyncHttpClient } from '../utils/axiosInstance';
import type {
  ApplyInboxResponse,
  BiometricEnrollmentRequest,
  BiometricVerificationRequest,
  BiometricVerificationResult,
  ChangeRequest,
  Citizen,
  CommitChangeResponse,
  CreateCitizenRequest,
  DepartmentUser,
  LedgerEntry,
  NodeInfo,
  PublishOutboxResponse,
  SubmitChangeRequest,
  SyncInboxEntry,
  SyncOutboxEvent,
  SyncReceipt,
} from './types';

export class CivicSyncClient {
  private readonly httpClient: ReturnType<typeof createCivicSyncHttpClient>;

  constructor(baseUrl: string) {
    this.httpClient = createCivicSyncHttpClient(baseUrl);
  }

  async getNodeInfo() {
    return this.get<NodeInfo>('/api/node');
  }

  async getCitizens() {
    return this.get<Citizen[]>('/api/citizens');
  }

  async createCitizen(request: CreateCitizenRequest) {
    return this.post<Citizen>('/api/citizens', request);
  }

  async enrollBiometric(citizenId: string, request: BiometricEnrollmentRequest) {
    return this.post<Citizen>(`/api/citizens/${citizenId}/biometrics/enroll`, request);
  }

  async verifyBiometric(citizenId: string, request: BiometricVerificationRequest) {
    return this.post<BiometricVerificationResult>(`/api/citizens/${citizenId}/biometrics/verify`, request);
  }

  async getDepartmentUsers() {
    return this.get<DepartmentUser[]>('/api/department-users');
  }

  async getChangeRequests() {
    return this.get<ChangeRequest[]>('/api/change-requests');
  }

  async submitChangeRequest(request: SubmitChangeRequest) {
    return this.post<ChangeRequest>('/api/change-requests', request);
  }

  async requestApproval(changeRequestId: string, approvingNodeId: string, approverUserId: string) {
    return this.post<ChangeRequest>(`/api/change-requests/${changeRequestId}/approvals`, {
      approvingNodeId,
      approverUserId,
    });
  }

  async recordDecision(changeRequestId: string, approvingNodeId: string, approverUserId: string, comment: string) {
    return this.post<ChangeRequest>(`/api/change-requests/${changeRequestId}/decisions`, {
      approvingNodeId,
      approverUserId,
      decision: 2,
      comment,
    });
  }

  async commitChangeRequest(changeRequestId: string) {
    return this.post<CommitChangeResponse>(`/api/change-requests/${changeRequestId}/commit`);
  }

  async publishOutbox() {
    return this.post<PublishOutboxResponse>('/api/sync/outbox/publish-pending');
  }

  async applyInbox() {
    return this.post<ApplyInboxResponse>('/api/sync/inbox/apply-pending');
  }

  async getLedger() {
    return this.get<LedgerEntry[]>('/api/audit/ledger');
  }

  async getOutbox() {
    return this.get<SyncOutboxEvent[]>('/api/audit/outbox');
  }

  async getInbox() {
    return this.get<SyncInboxEntry[]>('/api/audit/inbox');
  }

  async getReceipts() {
    return this.get<SyncReceipt[]>('/api/audit/sync-receipts');
  }

  private async get<T>(path: string): Promise<T> {
    const response = await this.httpClient.get<T>(path);
    return response.data;
  }

  private async post<T>(path: string, body?: unknown): Promise<T> {
    const response = await this.httpClient.post<T>(path, body);
    return response.data;
  }
}