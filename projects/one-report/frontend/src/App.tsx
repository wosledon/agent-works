// OneReport - 低代码报表工具
import './index.css'
import { Layout } from './components/Layout'
import { Sidebar } from './components/Sidebar'
import { Canvas } from './components/Canvas'
import { PropertyPanel } from './components/PropertyPanel'

function App() {
  return (
    <Layout>
      <div className="flex h-screen w-full">
        <Sidebar />
        <Canvas />
        <PropertyPanel />
      </div>
    </Layout>
  )
}

export default App