export interface VLMResponseContract {
  productName: string;
  rating: number; // 1-10
  pros: string[];
  cons: string[];
  summary: string;
  extractedIngredients: string;
}

export interface ScannedItem {
  id: string;
  query: string; // EAN or text search
  timestamp: number;
  result?: VLMResponseContract;
}
