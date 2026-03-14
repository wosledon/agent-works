import { Search } from 'lucide-react';
import type { ComponentProps, ComponentStyle } from '~/types';

interface FilterComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

export function FilterComponent({ props, style }: FilterComponentProps) {
  const { placeholder = '请输入筛选条件' } = props as { placeholder?: string };

  return (
    <div className="h-full flex items-center"
      style={{
        backgroundColor: style.backgroundColor || '#ffffff',
        borderRadius: style.borderRadius || 6,
        borderColor: style.borderColor || '#d1d5db',
        borderWidth: style.borderWidth || 1,
        borderStyle: 'solid',
        padding: style.padding || '0 12px',
      }}
    >
      <Search className="w-4 h-4 text-gray-400 mr-2 flex-shrink-0" />
      <input
        type="text"
        placeholder={placeholder}
        className="flex-1 bg-transparent outline-none text-sm w-full"
        style={{
          fontSize: style.fontSize || 14,
          color: style.color || '#374151',
        }}
        readOnly
      />
    </div>
  );
}
