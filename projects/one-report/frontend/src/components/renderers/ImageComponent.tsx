import { ImageIcon } from 'lucide-react';
import type { ComponentProps, ComponentStyle } from '~/types';

interface ImageComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

export function ImageComponent({ props, style }: ImageComponentProps) {
  const { src, alt = '图片' } = props as { src?: string; alt?: string };

  return (
    <div className="h-full flex items-center justify-center overflow-hidden rounded-lg"
      style={{
        backgroundColor: style.backgroundColor || '#f3f4f6',
        borderRadius: style.borderRadius || 8,
        borderColor: style.borderColor || '#e5e7eb',
        borderWidth: style.borderWidth || 1,
        borderStyle: 'solid',
      }}
    >
      {src ? (
        <img 
          src={src} 
          alt={alt} 
          className="w-full h-full object-cover"
        />
      ) : (
        <div className="flex flex-col items-center justify-center text-gray-400"
          style={{ padding: style.padding || 16 }}
        >
          <ImageIcon className="w-12 h-12 mb-2" />
          <span className="text-sm"
            style={{ fontSize: style.fontSize || 14, color: style.color }}
          >
            {alt as string}
          </span>
        </div>
      )}
    </div>
  );
}
