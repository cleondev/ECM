"use client";

import { useEffect } from "react";
import { registerSyncfusionLicense } from "@/lib/syncfusion-license";

export function SyncfusionLicenseLoader() {
  useEffect(() => {
    registerSyncfusionLicense();
  }, []);

  return null;
}
