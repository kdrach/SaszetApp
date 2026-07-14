export interface LlmProvider {
  id: string;
  providerName: string;
  modelName: string;
  isPrimary: boolean;
  isActive: boolean;
  priorityOrder: number;
}

export interface CreateProviderDto {
  providerName: string;
  modelName: string;
  apiKey: string;
  isPrimary: boolean;
  isActive: boolean;
}
