import { useState } from 'react';
import { Info, GitBranch, FileText, MessageSquare } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from './ui/tabs';
import { InfoTab } from './tabs/InfoTab';
import { FlowTab } from './tabs/FlowTab';
import { FormTab } from './tabs/FormTab';
import { ChatTab } from './tabs/ChatTab';

interface SidebarPanelProps {
  file: {
    name: string;
    type: string;
    size: number;
    owner: string;
    createdAt: string;
    modifiedAt: string;
    tags?: Array<{
      name: string;
      color: string;
    }>;
    sharedWith: Array<{
      name: string;
      email: string;
    }>;
    activeViewers?: Array<{
      name: string;
      email: string;
      status: string;
      color: string;
    }>;
  };
  onClose: () => void;
}

export function SidebarPanel({ file, onClose }: SidebarPanelProps) {
  const [activeTab, setActiveTab] = useState('info');

  return (
    <div className="w-80 bg-white border-l border-neutral-200 flex flex-col">
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <TabsList className="w-full grid grid-cols-4 rounded-none border-b border-neutral-200 bg-white h-12">
          <TabsTrigger value="info" className="flex flex-col gap-0.5 h-full data-[state=active]:bg-neutral-100">
            <Info className="h-4 w-4" />
            <span className="text-xs">Info</span>
          </TabsTrigger>
          <TabsTrigger value="flow" className="flex flex-col gap-0.5 h-full data-[state=active]:bg-neutral-100">
            <GitBranch className="h-4 w-4" />
            <span className="text-xs">Flow</span>
          </TabsTrigger>
          <TabsTrigger value="form" className="flex flex-col gap-0.5 h-full data-[state=active]:bg-neutral-100">
            <FileText className="h-4 w-4" />
            <span className="text-xs">Form</span>
          </TabsTrigger>
          <TabsTrigger value="chat" className="flex flex-col gap-0.5 h-full data-[state=active]:bg-neutral-100">
            <MessageSquare className="h-4 w-4" />
            <span className="text-xs">Chat</span>
          </TabsTrigger>
        </TabsList>

        <div className="flex-1 overflow-hidden">
          <TabsContent value="info" className="h-full m-0 overflow-y-auto">
            <InfoTab file={file} />
          </TabsContent>
          
          <TabsContent value="flow" className="h-full m-0 overflow-y-auto">
            <FlowTab />
          </TabsContent>
          
          <TabsContent value="form" className="h-full m-0 overflow-y-auto">
            <FormTab />
          </TabsContent>
          
          <TabsContent value="chat" className="h-full m-0">
            <ChatTab file={file} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}