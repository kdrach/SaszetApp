import { VLMResponseContract } from '../types';

// Mock responses for development
const mockResponses: Record<string, VLMResponseContract> = {
  'pl': {
    productName: "Premium Pet Food Wołowina",
    rating: 8,
    pros: ["Wysoka zawartość mięsa wołowego", "Brak zbóż", "Dodatek tauryny"],
    cons: ["Zawiera sztuczne barwniki"],
    summary: "Bardzo dobra karma z wysoką zawartością białka zwierzęcego. Idealna dla aktywnych kotów.",
    extractedIngredients: "Wołowina 65% (mięso, płuca, wątroba), bulion 30%, minerały 1%, olej z łososia 0.5%."
  },
  'en': {
    productName: "Premium Pet Food Beef",
    rating: 8,
    pros: ["High beef meat content", "Grain-free", "Added taurine"],
    cons: ["Contains artificial colors"],
    summary: "Very good pet food with high animal protein content. Ideal for active cats.",
    extractedIngredients: "Beef 65% (meat, lungs, liver), broth 30%, minerals 1%, salmon oil 0.5%."
  }
};

const badMockResponse: Record<string, VLMResponseContract> = {
  'pl': {
    productName: "Marketowa Karma z Kurczakiem",
    rating: 3,
    pros: ["Niska cena"],
    cons: ["Tylko 4% mięsa", "Wysoka zawartość zbóż i węglowodanów", "Brak podanego pochodzenia mięsa"],
    summary: "Słabej jakości karma oparta na zbożach z małą ilością produktów pochodzenia zwierzęcego. Unikać na dłuższą metę.",
    extractedIngredients: "Zboża (pszenica 40%), mięso i produkty pochodzenia zwierzęcego (w tym kurczak 4%), roślinne ekstrakty białkowe, oleje i tłuszcze, minerały."
  },
  'en': {
    productName: "Supermarket Chicken Food",
    rating: 3,
    pros: ["Low price"],
    cons: ["Only 4% meat content", "High grain and carbohydrate content", "Unspecified meat origin"],
    summary: "Poor quality grain-based food with low animal products content. Avoid for long term use.",
    extractedIngredients: "Cereals (wheat 40%), meat and animal derivatives (including 4% chicken), vegetable protein extracts, oils and fats, minerals."
  }
}

export const fetchAnalysisResult = async (query: string, language: string): Promise<VLMResponseContract> => {
  // Simulate 3-5 seconds of network & VLM processing delay
  return new Promise((resolve) => {
    setTimeout(() => {
      // Return a "bad" rating mock if the query contains the word "bad" or "złe"
      const langKey = language === 'pl' ? 'pl' : 'en';
      if (query.toLowerCase().includes('bad') || query.toLowerCase().includes('złe')) {
        resolve(badMockResponse[langKey]);
      } else {
        // Return good mock otherwise
        const response = mockResponses[langKey] || mockResponses['en'];
        // dynamically change the product name for realism based on the query if it's not a generic ID
        resolve({
          ...response,
          productName: query.length < 15 ? query.toUpperCase() : response.productName
        });
      }
    }, 4000); // 4 seconds delay
  });
};
