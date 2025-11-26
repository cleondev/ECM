import { useState } from 'react';
import { Toolbar } from './components/Toolbar';
import { FileViewer } from './components/FileViewer';
import { SidebarPanel } from './components/SidebarPanel';

// Mock file data
const mockFile = {
  id: '1',
  name: 'project-proposal-2024.pdf',
  type: 'application/pdf',
  size: 4857600, // 4.8 MB
  url: 'https://arxiv.org/pdf/2301.07041.pdf',
  owner: 'Nguyen Van A',
  createdAt: '2024-11-20T10:30:00Z',
  modifiedAt: '2024-11-22T14:15:00Z',
  tags: [
    { name: 'Documents', color: 'blue' },
    { name: 'Work', color: 'green' },
    { name: 'Important', color: 'red' }
  ],
  sharedWith: [
    { name: 'Tran Thi B', email: 'tranthib@example.com' },
    { name: 'Le Van C', email: 'levanc@example.com' }
  ],
  activeViewers: [
    { name: 'Bạn', email: 'you@example.com', status: 'active', color: 'blue' },
    { name: 'Tran Thi B', email: 'tranthib@example.com', status: 'active', color: 'green' },
    { name: 'Le Van C', email: 'levanc@example.com', status: 'viewing', color: 'purple' }
  ]
};

export default function App() {
  const [currentFile, setCurrentFile] = useState(mockFile);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);

  return (
    <div className="h-screen w-full flex flex-col bg-neutral-900">
      {/* Toolbar */}
      <Toolbar 
        file={currentFile}
        onSidebarToggle={() => setIsSidebarOpen(!isSidebarOpen)}
        isSidebarOpen={isSidebarOpen}
      />

      {/* Main Content Area */}
      <div className="flex-1 flex overflow-hidden">
        {/* File Viewer - Center (Main Focus) */}
        <div className="flex-1 overflow-auto bg-neutral-900">
          <FileViewer file={currentFile} />
        </div>

        {/* Sidebar Panel - Right */}
        {isSidebarOpen && (
          <SidebarPanel file={currentFile} onClose={() => setIsSidebarOpen(false)} />
        )}
      </div>
    </div>
  );
}