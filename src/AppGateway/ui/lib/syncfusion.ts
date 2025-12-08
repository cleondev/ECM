import { registerLicense } from "@syncfusion/ej2-base"

let licenseApplied = false

export function ensureSyncfusionLicense() {
  if (licenseApplied) {
    return
  }

  const licenseKey = process.env.NEXT_PUBLIC_SYNCFUSION_LICENSE_KEY
  if (licenseKey) {
    registerLicense(licenseKey)
    licenseApplied = true
  }
}
