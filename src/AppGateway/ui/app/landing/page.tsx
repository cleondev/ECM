"use client"

import { useEffect, useMemo, useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"

import { BrandLogo } from "@/components/brand-logo"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Sheet, SheetClose, SheetContent, SheetTrigger } from "@/components/ui/sheet"
import { checkLogin } from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import { normalizeRedirectTarget } from "@/lib/utils"
import {
  ArrowRight,
  BarChart3,
  CheckCircle2,
  Cloud,
  FileText,
  Menu,
  Search,
  Shield,
  Users,
  Zap,
} from "lucide-react"

import "./globals.css"

const navigationLinks = [
  { href: "#features", label: "Features" },
  { href: "#benefits", label: "Benefits" },
  { href: "#contact", label: "Contact" },
]

const heroVariants = [
  {
    title: "The complete platform for document management",
    body:
      "Securely store, organize, and collaborate on documents. Streamline your workflow with intelligent automation and enterprise-grade security.",
  },
  {
    title: "Everything you need to manage documents at scale",
    body: "Built for modern teams. Powerful features that help you work faster and smarter across every department.",
  },
]

const featureCards = [
  {
    title: "Analytics & Insights",
    description: "Track document usage, team productivity, and compliance with detailed analytics.",
    icon: BarChart3,
  },
  {
    title: "Intelligent Search",
    description: "Find any document instantly with AI-powered search across all your content and metadata.",
    icon: Search,
  },
  {
    title: "Enterprise Security",
    description: "Bank-level encryption, role-based access control, and compliance with global standards.",
    icon: Shield,
  },
  {
    title: "Workflow Automation",
    description: "Automate document routing, approvals, and notifications to save time and reduce errors.",
    icon: Zap,
  },
  {
    title: "Cloud Storage",
    description: "Unlimited scalable storage with automatic backups and disaster recovery built-in.",
    icon: Cloud,
  },
  {
    title: "Team Collaboration",
    description: "Work together seamlessly with version control, comments, and instant notifications.",
    icon: Users,
  },
]

const benefitHighlights = [
  {
    title: "AI-first experiences",
    description: "Smart summaries, auto-tagging, and proactive suggestions help everyone find answers faster.",
  },
  {
    title: "Security by design",
    description: "Granular permissions, detailed audit trails, and encryption keep sensitive content safe.",
  },
  {
    title: "Built for velocity",
    description: "Templates, automations, and collaboration tools reduce busywork so teams can ship faster.",
  },
]

export default function ECMLandingPage() {
  const router = useRouter()
  const [activeVariant, setActiveVariant] = useState(0)

  useEffect(() => {
    let isMounted = true

    const cached = getCachedAuthSnapshot()
    if (cached?.isAuthenticated) {
      router.replace(normalizeRedirectTarget(cached.redirectPath, "/app/"))
      return () => {
        isMounted = false
      }
    }

    checkLogin("/app/")
      .then((result) => {
        if (!isMounted || !result.isAuthenticated) {
          return
        }

        router.replace(normalizeRedirectTarget(result.redirectPath, "/app/"))
      })
      .catch((error) => {
        console.error("[landing] Unable to verify sign-in state", error)
      })

    return () => {
      isMounted = false
    }
  }, [router])

  useEffect(() => {
    const timer = setInterval(() => {
      setActiveVariant((current) => (current + 1) % heroVariants.length)
    }, 8000)

    return () => clearInterval(timer)
  }, [])

  const marqueeItems = useMemo(() => [...featureCards, ...featureCards], [])

  return (
    <div className="landing-theme min-h-screen flex flex-col bg-background">
      <header className="border-b border-border/40 bg-background/80 backdrop-blur-xl supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50 floating-header">
        <div className="container mx-auto px-4 lg:px-8">
          <div className="flex h-16 items-center justify-between gap-4 animate-fade-in">
            <Link href="/" className="flex items-center">
              <BrandLogo
                priority
                textClassName="text-xl font-semibold text-foreground"
                imageClassName="h-10 w-10"
              />
            </Link>
            <nav className="hidden md:flex items-center gap-8">
              {navigationLinks.map((link) => (
                <a
                  key={link.href}
                  href={link.href}
                  className="text-sm text-muted-foreground hover:text-primary hover:scale-105 transition-all"
                >
                  {link.label}
                </a>
              ))}
            </nav>
            <div className="hidden md:flex items-center gap-3">
              <Link href="/signin/?returnUrl=/app/">
                <Button variant="ghost" size="sm" className="text-foreground hover:text-primary hover:scale-105">
                  Sign In
                </Button>
              </Link>
              <Button
                size="sm"
                className="bg-primary text-primary-foreground hover:bg-primary/90 hover:scale-105 hover:shadow-lg hover:shadow-primary/50 shimmer"
              >
                Get Started
              </Button>
            </div>
            <Sheet>
              <SheetTrigger asChild>
                <Button
                  variant="outline"
                  size="icon"
                  className="md:hidden border-border text-foreground hover:text-primary"
                  aria-label="Open navigation menu"
                >
                  <Menu className="h-5 w-5" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="bg-background/95 backdrop-blur p-6 sm:max-w-sm">
                <div className="flex flex-col gap-6">
                  <div className="flex items-center justify-between">
                    <BrandLogo
                      priority
                      textClassName="text-lg font-semibold text-foreground"
                      imageClassName="h-9 w-9"
                    />
                  </div>
                  <nav className="flex flex-col gap-4">
                    {navigationLinks.map((link) => (
                      <SheetClose asChild key={link.href}>
                        <a
                          href={link.href}
                          className="text-base text-muted-foreground hover:text-primary"
                        >
                          {link.label}
                        </a>
                      </SheetClose>
                    ))}
                  </nav>
                  <div className="flex flex-col gap-3 pt-4">
                    <SheetClose asChild>
                      <Link href="/signin/?returnUrl=/app/">
                        <Button variant="outline" className="w-full border-border text-foreground hover:text-primary">
                          Sign In
                        </Button>
                      </Link>
                    </SheetClose>
                    <Button className="w-full bg-primary text-primary-foreground hover:bg-primary/90 shimmer">
                      Get Started
                    </Button>
                  </div>
                </div>
              </SheetContent>
            </Sheet>
          </div>
        </div>
      </header>

      <main className="flex-1">
        <section className="relative overflow-hidden border-b border-border/40 stars-bg cosmic-glow">
          <div className="container mx-auto px-4 lg:px-8 py-16 sm:py-24 lg:py-32 relative z-10">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 lg:gap-14 items-center">
              <div className="space-y-8">
                <div className="inline-flex items-center gap-2 rounded-full bg-primary/10 px-4 py-2 text-sm text-primary ring-1 ring-primary/30">
                  <FileText className="h-4 w-4" />
                  <span>Document intelligence reimagined</span>
                </div>
                <div className="relative h-[220px] sm:h-[200px]" aria-live="polite">
                  {heroVariants.map((variant, index) => (
                    <div
                      key={variant.title}
                      className={`${index === activeVariant ? "opacity-100 translate-y-0" : "pointer-events-none opacity-0 translate-y-4"} transition-all duration-700 ease-out absolute left-0 right-0`}
                    >
                      <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold text-foreground leading-tight text-balance">
                        {variant.title}
                      </h1>
                      <p className="mt-4 text-lg sm:text-xl text-muted-foreground max-w-2xl text-pretty">
                        {variant.body}
                      </p>
                    </div>
                  ))}
                </div>
                <div className="flex flex-col sm:flex-row items-center gap-4">
                  <Button
                    size="lg"
                    className="w-full sm:w-auto bg-primary text-primary-foreground hover:bg-primary/90 hover:scale-105 sm:hover:scale-110 hover:shadow-2xl hover:shadow-primary/50 text-base px-8 transition-all shimmer"
                  >
                    Start Free Trial
                    <ArrowRight className="ml-2 h-5 w-5" />
                  </Button>
                  <Button
                    size="lg"
                    variant="outline"
                    className="w-full sm:w-auto text-base px-8 border-border text-foreground hover:bg-secondary hover:scale-105 sm:hover:scale-110 hover:shadow-xl bg-transparent transition-all"
                  >
                    Watch Demo
                  </Button>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  {benefitHighlights.map((benefit) => (
                    <div key={benefit.title} className="rounded-2xl border border-border/50 bg-background/40 p-4 shadow-sm">
                      <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                        <CheckCircle2 className="h-4 w-4 text-primary" />
                        {benefit.title}
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground leading-relaxed">{benefit.description}</p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="relative">
                <div className="glow-card relative overflow-hidden rounded-3xl border border-border/60 bg-gradient-to-b from-background/80 via-background/60 to-background/40 shadow-2xl">
                  <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_20%,rgba(99,102,241,0.18),transparent_45%),radial-gradient(circle_at_80%_0%,rgba(59,130,246,0.22),transparent_40%)]" />
                  <div className="relative p-8 lg:p-10 space-y-6">
                    <div className="flex items-center justify-between">
                      <div className="text-sm font-semibold text-muted-foreground">Live compliance overview</div>
                      <div className="rounded-full bg-primary/10 px-3 py-1 text-xs text-primary">Synced</div>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                      <Card className="bg-background/80 border-border/60">
                        <CardHeader className="pb-2">
                          <CardDescription>Document uptime</CardDescription>
                          <CardTitle className="text-3xl">99.99%</CardTitle>
                        </CardHeader>
                        <CardContent className="text-sm text-muted-foreground">Always-on storage across regions.</CardContent>
                      </Card>
                      <Card className="bg-background/80 border-border/60">
                        <CardHeader className="pb-2">
                          <CardDescription>Average approval</CardDescription>
                          <CardTitle className="text-3xl">2.4h</CardTitle>
                        </CardHeader>
                        <CardContent className="text-sm text-muted-foreground">Automated routing keeps work moving.</CardContent>
                      </Card>
                    </div>
                    <div className="rounded-2xl border border-border/60 bg-background/70 p-5 space-y-3">
                      <div className="flex items-center justify-between text-sm font-semibold">
                        <span>Security posture</span>
                        <span className="text-primary">Healthy</span>
                      </div>
                      <div className="h-2 rounded-full bg-border/60">
                        <div className="h-full w-[82%] rounded-full bg-gradient-to-r from-primary to-accent" />
                      </div>
                      <ul className="grid grid-cols-2 gap-2 text-sm text-muted-foreground">
                        <li className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-primary" /> SOC 2 controls</li>
                        <li className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-primary" /> SSO + MFA</li>
                        <li className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-primary" /> Audit trails</li>
                        <li className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-primary" /> Data residency</li>
                      </ul>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div
            className="pointer-events-none absolute top-1/4 left-1/4 w-96 h-96 max-w-[70vw] max-h-[70vw] bg-primary/10 rounded-full blur-3xl animate-pulse"
            aria-hidden
          />
          <div
            className="pointer-events-none absolute bottom-1/4 right-1/4 w-96 h-96 max-w-[70vw] max-h-[70vw] bg-accent/10 rounded-full blur-3xl animate-pulse"
            style={{ animationDelay: "2s" }}
            aria-hidden
          />
        </section>

        <section id="features" className="border-b border-border/40 stars-bg">
          <div className="container mx-auto px-4 lg:px-8 py-16 lg:py-24 relative z-10">
            <div className="section-surface px-6 sm:px-10 py-10 sm:py-14 lg:py-16">
              <div className="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
                <div className="max-w-2xl space-y-3">
                  <p className="text-sm font-semibold text-primary">Capabilities</p>
                  <h2 className="text-3xl sm:text-4xl font-bold text-foreground">Modern tooling for every document workflow</h2>
                  <p className="text-base text-muted-foreground">
                    Each capability is built with performance, security, and delightful UX in mind. Everything runs on our Next.js + Tailwind stack with no external CDNs required.
                  </p>
                </div>
                <div className="text-sm text-muted-foreground">
                  Hover over cards to pause the carousel. Fully responsive and keyboard friendly.
                </div>
              </div>

              <div className="mt-10">
                <div className="relative overflow-hidden rounded-3xl border border-border/60 bg-background/30 marquee">
                  <div className="absolute inset-y-0 left-0 w-16 bg-gradient-to-r from-background to-transparent pointer-events-none" />
                  <div className="absolute inset-y-0 right-0 w-16 bg-gradient-to-l from-background to-transparent pointer-events-none" />
                  <div className="marquee-track">
                    {marqueeItems.map((feature, index) => {
                      const Icon = feature.icon
                      return (
                        <Card
                          key={`${feature.title}-${index}`}
                          className="mx-4 my-6 w-[280px] flex-shrink-0 bg-background/70 border-border/60 shadow-lg hover:shadow-primary/30 transition-all hover:-translate-y-1"
                        >
                          <CardHeader className="space-y-3">
                            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
                              <Icon className="h-6 w-6" />
                            </div>
                            <CardTitle className="text-xl font-semibold text-foreground">{feature.title}</CardTitle>
                            <CardDescription className="text-muted-foreground text-sm leading-relaxed">
                              {feature.description}
                            </CardDescription>
                          </CardHeader>
                        </Card>
                      )
                    })}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="benefits" className="border-b border-border/40 bg-background">
          <div className="container mx-auto px-4 lg:px-8 py-16 lg:py-24">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 lg:gap-10">
              <div className="lg:col-span-1 space-y-4">
                <p className="text-sm font-semibold text-primary">Why teams choose ECM</p>
                <h2 className="text-3xl sm:text-4xl font-bold text-foreground">Designed for adoption, tuned for scale</h2>
                <p className="text-base text-muted-foreground">
                  Ship secure document experiences without piecing together libraries. Our stack is aligned with Next.js 15, Tailwind CSS, and Shadcn components already used across the product.
                </p>
              </div>
              <div className="lg:col-span-2 grid grid-cols-1 md:grid-cols-3 gap-4 lg:gap-6">
                {benefitHighlights.map((benefit) => (
                  <Card key={benefit.title} className="bg-background/70 border-border/60 shadow-sm hover:shadow-lg transition-all">
                    <CardHeader>
                      <CardTitle className="text-xl">{benefit.title}</CardTitle>
                      <CardDescription className="text-muted-foreground text-sm leading-relaxed">
                        {benefit.description}
                      </CardDescription>
                    </CardHeader>
                  </Card>
                ))}
              </div>
            </div>
            <div className="mt-10 grid grid-cols-1 md:grid-cols-3 gap-4 lg:gap-6">
              {featureCards.slice(0, 3).map((feature) => {
                const Icon = feature.icon
                return (
                  <Card key={feature.title} className="bg-background/70 border-border/60 shadow-sm">
                    <CardHeader className="space-y-2">
                      <div className="flex items-center gap-3 text-primary">
                        <Icon className="h-5 w-5" />
                        <span className="text-sm font-semibold">{feature.title}</span>
                      </div>
                      <CardTitle className="text-2xl text-foreground">Built-in best practices</CardTitle>
                      <CardDescription className="text-muted-foreground text-sm leading-relaxed">
                        {feature.description}
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                      <span className="rounded-full bg-primary/10 px-3 py-1 text-primary">No external CDNs</span>
                      <span className="rounded-full bg-secondary/20 px-3 py-1 text-foreground">Accessible defaults</span>
                      <span className="rounded-full bg-accent/10 px-3 py-1 text-accent-foreground">Type-safe</span>
                    </CardContent>
                  </Card>
                )
              })}
            </div>
          </div>
        </section>

        <section id="contact" className="py-14 bg-gradient-to-b from-background via-background/90 to-background/70">
          <div className="container mx-auto px-4 lg:px-8">
            <div className="section-surface px-6 sm:px-10 py-10 sm:py-14 lg:py-16 text-center space-y-6">
              <p className="text-sm font-semibold text-primary">Ready to launch</p>
              <h2 className="text-3xl sm:text-4xl font-bold text-foreground">Bring your next document experience online faster</h2>
              <p className="text-base text-muted-foreground max-w-2xl mx-auto">
                Start with the same Next.js + Tailwind foundation as the rest of ECM. Connect authentication, automation, and collaboration features without extra scripts or styling rewrites.
              </p>
              <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
                <Button size="lg" className="bg-primary text-primary-foreground hover:bg-primary/90 shimmer">
                  Talk to our team
                </Button>
                <Link href="/docs" className="text-sm text-primary hover:text-primary/80">
                  View documentation
                </Link>
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer className="site-footer text-center text-sm text-muted-foreground border-t border-border/40 py-4 bg-background/70">
        © {new Date().getFullYear()} ECM. Built with Next.js, Tailwind CSS, and our shared component library.
      </footer>
    </div>
  )
}
