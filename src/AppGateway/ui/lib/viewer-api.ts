import { gatewayRequest } from "./api"
import type { ViewerDescriptor, ViewerDescriptorDto } from "./viewer-types"
import { toViewerDescriptor } from "./viewer-types"

export async function fetchViewerDescriptor(versionId: string): Promise<ViewerDescriptor> {
  if (!versionId) {
    throw new Error("Version identifier is required")
  }

  const response = await gatewayRequest<ViewerDescriptorDto>(`/api/viewer/${versionId}`)
  return toViewerDescriptor(response)
}
