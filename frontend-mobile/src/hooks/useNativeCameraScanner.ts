import { useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { compressImage } from '../utils/imageUtils';

export function useNativeCameraScanner() {
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const triggerCamera = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    
    if (file) {
      try {
        const compressedBlob = await compressImage(file);
        navigate('/product/photo', { state: { imageBlob: compressedBlob } });
      } catch (error) {
        console.error('Failed to compress image:', error);
      }
    }
  };

  return {
    fileInputRef,
    triggerCamera,
    handleFileChange,
  };
}
