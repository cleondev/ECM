"use client";
import { registerLicense } from "@syncfusion/ej2-base";

export function registerSyncfusionLicense() {
  const key = process.env.NEXT_PUBLIC_SYNCFUSION_KEY;

  if (!key) {
    console.warn("Syncfusion license key is missing.");
    return;
  }

  registerLicense(key);
}
