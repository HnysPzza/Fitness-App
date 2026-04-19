StrideX: Next-Gen Fitness Tracking
Introducing StrideX, a next-generation fitness tracking experience engineered for the modern athlete. This release brings a complete, premium design overhaul featuring a sleek, responsive glassmorphic UI and custom 3D iconography. We've supercharged our live workout recording engine with real-time Mapbox GPS rendering, interactive state control, and background session resilience so you never lose a step. Built for both speed and aesthetics, StrideX gives you dynamic performance stats—like pace, distance, and time—wrapped in a beautiful, distraction-free environment that helps you stay laser-focused on crushing your goals.

✨ Features
Mapbox-Powered Location Intelligence

Real-time GPS tracking and dynamic routing via Mapbox APIs.
Interactive 3D maps with collapsible floating map control sheets.
On-the-fly Map Style switching (Outdoors, Streets, Satellite, Dark mode).
Advanced Workout Recording

Responsive, fluid recording controls (Start, Pause, Resume, Finish) with modern pulse animations.
Live-updating telemetry: current pace, total distance, and elapsed time.
Background GPS tracking utilizing robust Android Foreground Services, ensuring your workout is mapped seamlessly even when the app is minimized.
Select and tag multiple sport configurations dynamically.
Analytics & Progression

In-depth You Page and Home Page dashboards using interactive charts to visualize your weekly progress and long-term health metrics.
Thorough Activity Detail Pages for a post-workout breakdown of your routes and analytics.
Security & Account Customization

Industry-standard Supabase Authentication with robust Login, Registration, and Multi-Account switching.
Deep security integration featuring Two-Factor Authentication (2FA) and Email Verification.
Unparalleled personalization: Customize your Theme (Light/Dark), Accent Colors, Font Sizes, Units of measurement, and Date/Time formatting.
Ecosystem & Hardware Integration

Dedicated workflow for connecting Wearable Devices and other Linked Health Apps.
Manage Workout Reminders and push notifications dynamically.
Full control over Privacy and Data Sharing, empowering you to download "My Data" on demand.
🚀 How to Use StrideX
1. Getting Started Make sure your API Keys are configured locally before building.

Generate your Mapbox and Supabase keys.
Rename AppSecrets.Local.cs.example (located in the Services/ folder) to AppSecrets.Local.cs and paste your keys inside. Note: this file is completely hidden by .gitignore to protect your credentials.
Run a clean dotnet build or deploy directly to your emulator/device.
2. Starting a Workout

Navigate to the Record tab on the bottom navigation bar.
Use the Sport Selector Pill on the bottom left to choose your activity (Running, Walking, Cycling, etc).
If you want to change your map aesthetic, tap the Layers icon on the right to pull up the Map Style drawer, or tap 3D to tilt the camera.
Hit the glowing orange Record button in the center to begin tracking.
3. During Your Workout

Your metrics (Pace, Distance, Time) will actively compile in the glassmorphic card layered at the bottom of the screen.
You can put your phone to sleep or switch apps safely; StrideX's Foreground Service will continue polling your route and will plant a sticky notification in your tray for easy return.
Tap the Pause button to freeze tracking. The UI seamlessly transitions, exposing the Finish button on the left.
