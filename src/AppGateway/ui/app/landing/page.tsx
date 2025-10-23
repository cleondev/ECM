"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { checkLogin } from "@/lib/api"
import { FileText, Search, Shield, Users, Zap, Lock, Cloud, BarChart3, ArrowRight, CheckCircle2 } from "lucide-react"

export default function ECMLandingPage() {
  const router = useRouter()

  useEffect(() => {
    let isMounted = true

    checkLogin("/app")
      .then(result => {
        if (!isMounted) {
          return
        }

        if (result.isAuthenticated) {
          router.replace("/app")
        }
      })
      .catch(() => {
        /* landing page vẫn hiển thị nếu gọi API lỗi */
      })

    return () => {
      isMounted = false
    }
  }, [router])

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b border-border/40 bg-background/80 backdrop-blur-xl supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50">
        <div className="container mx-auto px-4 lg:px-8">
          <div className="flex h-16 items-center justify-between animate-fade-in">
            <div className="flex items-center gap-2">
              <FileText className="h-6 w-6 text-primary" />
              <span className="text-xl font-semibold text-foreground">ECM</span>
            </div>
            <nav className="hidden md:flex items-center gap-8">
              <a
                href="#features"
                className="text-sm text-muted-foreground hover:text-primary hover:scale-105 transition-all"
              >
                Features
              </a>
              <a
                href="#solutions"
                className="text-sm text-muted-foreground hover:text-primary hover:scale-105 transition-all"
              >
                Solutions
              </a>
              <a
                href="#pricing"
                className="text-sm text-muted-foreground hover:text-primary hover:scale-105 transition-all"
              >
                Pricing
              </a>
              <a
                href="#contact"
                className="text-sm text-muted-foreground hover:text-primary hover:scale-105 transition-all"
              >
                Contact
              </a>
            </nav>
            <div className="flex items-center gap-3">
            <Link href="/signin?redirectUri=/app">
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
          </div>
        </div>
      </header>

      <section className="relative overflow-hidden border-b border-border/40 stars-bg cosmic-glow">
        <div className="container mx-auto px-4 lg:px-8 py-32 lg:py-40 relative z-10">
          <div className="max-w-4xl mx-auto text-center animate-slide-up">
            <h1 className="text-6xl lg:text-8xl font-bold text-foreground mb-6 text-balance leading-tight">
              The complete platform for <span className="gradient-text">document management</span>
            </h1>
            <p className="text-xl lg:text-2xl text-muted-foreground mb-12 max-w-2xl mx-auto text-pretty leading-relaxed">
              Securely store, organize, and collaborate on documents. Streamline your workflow with intelligent
              automation and enterprise-grade security.
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <Button
                size="lg"
                className="bg-primary text-primary-foreground hover:bg-primary/90 hover:scale-110 hover:shadow-2xl hover:shadow-primary/50 text-base px-8 transition-all shimmer"
              >
                Start Free Trial
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
              <Button
                size="lg"
                variant="outline"
                className="text-base px-8 border-border text-foreground hover:bg-secondary hover:scale-110 hover:shadow-xl bg-transparent transition-all"
              >
                Watch Demo
              </Button>
            </div>
          </div>
        </div>
        <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-primary/10 rounded-full blur-3xl animate-pulse"></div>
        <div
          className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-accent/10 rounded-full blur-3xl animate-pulse"
          style={{ animationDelay: "2s" }}
        ></div>
      </section>

      <section className="border-b border-border/40 stars-bg">
        <div className="container mx-auto px-4 lg:px-8 py-20 relative z-10">
          <p className="text-center text-sm text-muted-foreground mb-10 animate-fade-in">
            Trusted by leading organizations worldwide
          </p>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 items-center justify-items-center max-w-4xl mx-auto">
            {[
              { stat: "50K+", label: "Active Users" },
              { stat: "99.9%", label: "Uptime SLA" },
              { stat: "10M+", label: "Documents Managed" },
              { stat: "150+", label: "Countries" },
            ].map((item, i) => (
              <div
                key={i}
                className="text-center animate-scale-in hover:scale-110 transition-transform cursor-default"
                style={{ animationDelay: `${i * 0.1}s` }}
              >
                <div className="text-4xl font-bold gradient-text mb-2">{item.stat}</div>
                <div className="text-sm text-muted-foreground">{item.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section id="features" className="border-b border-border/40 stars-bg cosmic-glow">
        <div className="container mx-auto px-4 lg:px-8 py-28 relative z-10">
          <div className="max-w-3xl mb-20 animate-slide-up">
            <h2 className="text-5xl lg:text-6xl font-bold text-foreground mb-6 text-balance">
              Everything you need to manage documents at <span className="gradient-text">scale</span>
            </h2>
            <p className="text-xl text-muted-foreground text-pretty leading-relaxed">
              Built for modern teams. Powerful features that help you work faster and smarter.
            </p>
          </div>

          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[
              {
                icon: Search,
                title: "Intelligent Search",
                description: "Find any document instantly with AI-powered search across all your content and metadata.",
              },
              {
                icon: Shield,
                title: "Enterprise Security",
                description: "Bank-level encryption, role-based access control, and compliance with global standards.",
              },
              {
                icon: Users,
                title: "Real-time Collaboration",
                description: "Work together seamlessly with version control, comments, and instant notifications.",
              },
              {
                icon: Zap,
                title: "Workflow Automation",
                description: "Automate document routing, approvals, and notifications to save time and reduce errors.",
              },
              {
                icon: Cloud,
                title: "Cloud Storage",
                description: "Unlimited scalable storage with automatic backups and disaster recovery built-in.",
              },
              {
                icon: BarChart3,
                title: "Analytics & Insights",
                description: "Track document usage, team productivity, and compliance with detailed analytics.",
              },
            ].map((feature, i) => (
              <Card
                key={i}
                className="p-8 bg-card/50 backdrop-blur-sm border-border hover:border-primary/50 hover:shadow-2xl hover:shadow-primary/20 hover:scale-105 transition-all duration-500 animate-fade-in float-animation"
                style={{ animationDelay: `${i * 0.15}s` }}
              >
                <feature.icon className="h-12 w-12 text-primary mb-6 transition-transform group-hover:scale-110" />
                <h3 className="text-xl font-semibold text-foreground mb-3">{feature.title}</h3>
                <p className="text-muted-foreground leading-relaxed">{feature.description}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      <section id="solutions" className="border-b border-border/40 stars-bg">
        <div className="container mx-auto px-4 lg:px-8 py-28 relative z-10">
          <div className="grid lg:grid-cols-2 gap-20 items-center">
            <div className="animate-slide-up">
              <h2 className="text-5xl lg:text-6xl font-bold text-foreground mb-8 text-balance">
                Built for <span className="gradient-text">every department</span>
              </h2>
              <p className="text-xl text-muted-foreground mb-10 leading-relaxed">
                From HR to legal, finance to operations—ECM adapts to your team's unique workflows and
                requirements.
              </p>
              <div className="space-y-5">
                {[
                  "Centralized document repository with smart organization",
                  "Automated compliance and audit trails",
                  "Seamless integration with existing tools",
                  "Mobile access for teams on the go",
                ].map((item, i) => (
                  <div
                    key={i}
                    className="flex items-start gap-4 hover:translate-x-2 transition-transform duration-300 animate-fade-in"
                    style={{ animationDelay: `${i * 0.1}s` }}
                  >
                    <CheckCircle2 className="h-6 w-6 text-primary mt-0.5 flex-shrink-0" />
                    <span className="text-foreground text-lg">{item}</span>
                  </div>
                ))}
              </div>
              <Button
                size="lg"
                className="mt-10 bg-primary text-primary-foreground hover:bg-primary/90 hover:scale-110 hover:shadow-2xl hover:shadow-primary/50 transition-all shimmer"
              >
                Explore Solutions
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </div>
            <div className="relative animate-scale-in">
              <Card className="p-10 bg-card/50 backdrop-blur-sm border-border hover:shadow-2xl hover:shadow-primary/20 transition-all duration-500 float-animation">
                <div className="space-y-6">
                  {[
                    { icon: Lock, title: "Secure IAM", desc: "Role-based permissions" },
                    { icon: FileText, title: "Version History", desc: "Track all changes" },
                    { icon: Users, title: "Team Collaboration", desc: "Work together in real-time" },
                  ].map((item, i) => (
                    <div
                      key={i}
                      className="flex items-center gap-5 p-5 bg-secondary/30 backdrop-blur-sm rounded-xl hover:bg-secondary/50 hover:scale-105 transition-all duration-300 border border-border/30"
                      style={{ animationDelay: `${i * 0.15}s` }}
                    >
                      <item.icon className="h-10 w-10 text-primary" />
                      <div>
                        <div className="font-semibold text-foreground text-lg">{item.title}</div>
                        <div className="text-sm text-muted-foreground">{item.desc}</div>
                      </div>
                    </div>
                  ))}
                </div>
              </Card>
            </div>
          </div>
        </div>
      </section>

      <section className="border-b border-border/40 stars-bg cosmic-glow">
        <div className="container mx-auto px-4 lg:px-8 py-32 relative z-10">
          <div className="max-w-3xl mx-auto text-center animate-slide-up">
            <h2 className="text-5xl lg:text-6xl font-bold text-foreground mb-8 text-balance">
              Ready to transform your <span className="gradient-text">document management</span>?
            </h2>
            <p className="text-xl text-muted-foreground mb-12 leading-relaxed">
              Join thousands of organizations that trust ECM to manage their critical documents securely and
              efficiently.
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <Button
                size="lg"
                className="bg-primary text-primary-foreground hover:bg-primary/90 hover:scale-110 hover:shadow-2xl hover:shadow-primary/50 text-base px-8 transition-all shimmer"
              >
                Start Free Trial
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
              <Button
                size="lg"
                variant="outline"
                className="text-base px-8 border-border text-foreground hover:bg-secondary hover:scale-110 hover:shadow-xl bg-transparent transition-all"
              >
                Contact Sales
              </Button>
            </div>
            <p className="text-sm text-muted-foreground mt-8">
              No credit card required • 14-day free trial • Cancel anytime
            </p>
          </div>
        </div>
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-primary/5 rounded-full blur-3xl animate-pulse"></div>
      </section>

      <footer className="stars-bg">
        <div className="container mx-auto px-4 lg:px-8 py-16 relative z-10">
          <div className="grid md:grid-cols-4 gap-10 mb-12">
            <div>
              <div className="flex items-center gap-2 mb-4">
                <FileText className="h-6 w-6 text-primary" />
                <span className="font-semibold text-foreground text-lg">ECM</span>
              </div>
              <p className="text-sm text-muted-foreground leading-relaxed">
                Enterprise content management made simple and secure.
              </p>
            </div>
            <div>
              <h4 className="font-semibold text-foreground mb-4">Product</h4>
              <ul className="space-y-3 text-sm text-muted-foreground">
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Features
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Pricing
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Security
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Integrations
                  </a>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-foreground mb-4">Company</h4>
              <ul className="space-y-3 text-sm text-muted-foreground">
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    About
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Blog
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Careers
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Contact
                  </a>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-foreground mb-4">Legal</h4>
              <ul className="space-y-3 text-sm text-muted-foreground">
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Privacy
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Terms
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:text-primary hover:translate-x-1 inline-block transition-all">
                    Compliance
                  </a>
                </li>
              </ul>
            </div>
          </div>
          <div className="border-t border-border pt-10 text-center text-sm text-muted-foreground">
            <p>© 2025 ECM. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  )
}
