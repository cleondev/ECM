import { Plus, Play, Settings } from 'lucide-react';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Separator } from '../ui/separator';

interface FlowStep {
  id: string;
  name: string;
  status: 'pending' | 'active' | 'completed';
  assignee?: string;
}

const mockFlowSteps: FlowStep[] = [
  { id: '1', name: 'Design Review', status: 'completed', assignee: 'Nguyen Van A' },
  { id: '2', name: 'Development', status: 'active', assignee: 'Tran Thi B' },
  { id: '3', name: 'QA Testing', status: 'pending', assignee: 'Le Van C' },
  { id: '4', name: 'Deployment', status: 'pending' }
];

export function FlowTab() {
  const getStatusColor = (status: FlowStep['status']) => {
    switch (status) {
      case 'completed':
        return 'bg-green-500';
      case 'active':
        return 'bg-blue-500';
      default:
        return 'bg-neutral-300';
    }
  };

  const getStatusText = (status: FlowStep['status']) => {
    switch (status) {
      case 'completed':
        return 'Hoàn thành';
      case 'active':
        return 'Đang thực hiện';
      default:
        return 'Chờ xử lý';
    }
  };

  return (
    <div className="p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm">Quy trình làm việc</h3>
        <Button variant="ghost" size="sm" className="h-7 text-xs">
          <Settings className="h-3 w-3 mr-1" />
          Cấu hình
        </Button>
      </div>

      <div className="space-y-3">
        {mockFlowSteps.map((step, index) => (
          <div key={step.id}>
            <div className="flex gap-3">
              <div className="flex flex-col items-center">
                <div className={`w-6 h-6 rounded-full flex items-center justify-center text-white text-xs ${getStatusColor(step.status)}`}>
                  {step.status === 'completed' ? '✓' : index + 1}
                </div>
                {index < mockFlowSteps.length - 1 && (
                  <div className="w-0.5 h-8 bg-neutral-200 my-1" />
                )}
              </div>
              <div className="flex-1 pb-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1">
                    <div className="text-sm">{step.name}</div>
                    {step.assignee && (
                      <div className="text-xs text-neutral-500 mt-0.5">
                        {step.assignee}
                      </div>
                    )}
                  </div>
                  <Badge 
                    variant={step.status === 'active' ? 'default' : 'secondary'}
                    className="text-xs shrink-0"
                  >
                    {getStatusText(step.status)}
                  </Badge>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <Separator />

      <div className="space-y-2">
        <Button variant="outline" size="sm" className="w-full text-xs">
          <Plus className="h-3 w-3 mr-1" />
          Thêm bước mới
        </Button>
        <Button size="sm" className="w-full text-xs">
          <Play className="h-3 w-3 mr-1" />
          Bắt đầu quy trình
        </Button>
      </div>
    </div>
  );
}
