import type { ComponentProps, ComponentStyle } from '~/types';

interface TextComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
  isPreview?: boolean;
}

export function TextComponent({ props, style }: TextComponentProps) {
  const { text = '文本内容' } = props;

  return (
    <div className="h-full flex items-center justify-center p-2"
      style={{
        backgroundColor: style.backgroundColor || 'transparent',
        borderRadius: style.borderRadius || 0,
        borderColor: style.borderColor,
        borderWidth: style.borderWidth || 0,
        borderStyle: 'solid',
      }}
    >
      <span
        style={{
          fontSize: style.fontSize || 14,
          color: style.color || '#374151',
          padding: style.padding || 8,
        }}
      >
        {text}
      </span>
    </div>
  );
}
