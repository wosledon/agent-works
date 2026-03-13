// OneReport - 低代码报表工具
import './index.css';
import { Layout } from './components/Layout';
import { Sidebar } from './components/Sidebar';
import { Canvas } from './components/Canvas';
import { PropertyPanel } from './components/PropertyPanel';
import { useReportStore } from './store';

function App() {
  const isPreview = useReportStore((state) => state.isPreview);

  return (
    <Layout>
      <div className="flex flex-1 overflow-hidden">
        {!isPreview && <Sidebar />}
        <Canvas />
        
        {!isPreview && <PropertyPanel />}
      </div>
    </Layout>
  );
}

export default App;
