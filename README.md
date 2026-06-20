# Resume ATS Analyzer

A full-stack ATS (Applicant Tracking System) resume analyzer.

🔗 **Live Links:**
- **Frontend App**: [https://kalyan-b-resume-analyzer.vercel.app/](https://kalyan-b-resume-analyzer.vercel.app/)
- **Backend API**: [https://kalyan-b-resume-analyzer.onrender.com](https://kalyan-b-resume-analyzer.onrender.com)

Built with:
- **Backend**: ASP.NET Core (.NET 8) Web API
- **Frontend**: React + Vite + TypeScript + Tailwind CSS
- **AI**: Groq API (free tier available)

---

## Features

- Upload PDF or DOCX resume files
- Instant AI-powered ATS score (0–100)
- Detects tech stack, strengths, keyword gaps, and weaknesses
- Actionable improvement suggestions and career advice
- Caches analysis results in-memory for repeated uploads

---

## Project Structure

```
resume-ats/
├── backend/          # ASP.NET Core .NET 8 Web API
│   ├── Controllers/  # AnalyzeController (API endpoints)
│   ├── Services/     # ResumeAnalyzerService (Groq AI integration)
│   ├── Models/       # ResumeAnalysis, AnalyzeResponse
│   ├── Repository/   # InMemoryAnalysisRepository (caching)
│   ├── Config/       # GroqSettings (configuration)
│   ├── Helpers/      # HashHelper
│   └── Enums/        # ExperienceLevel, FileType
└── frontend/         # React + Vite frontend
    └── src/
        ├── components/home/  # ResumeAnalyzer, ATSDashboard, FileUpload
        ├── lib/extractors/   # PDF and DOCX text extraction
        └── types/            # TypeScript types
```

---

## Setup & Running Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- A free [Groq API key](https://console.groq.com)

### 1. Clone the repository

```bash
git clone https://github.com/kalyan-reddy-b/resume-ats.git
cd resume-ats
```

### 2. Set up the GROQ API key (IMPORTANT — keep this secret!)

**Option A: Environment variable (recommended)**

Windows PowerShell:
```powershell
$env:GROQ_API_KEY="your_groq_api_key_here"
```

Linux/macOS:
```bash
export GROQ_API_KEY="your_groq_api_key_here"
```

**Option B: `appsettings.Development.json` (local only, gitignored)**

Create `backend/appsettings.Development.json` (already in `.gitignore`):
```json
{
  "GroqSettings": {
    "ApiKey": "your_groq_api_key_here"
  }
}
```

> ⚠️ **Never put your API key in `appsettings.json`** — that file is committed to git!

### 3. Run the backend

```bash
cd backend
dotnet restore
dotnet run
```

The backend will start at `http://localhost:5069`.

### 4. Run the frontend

Open a new terminal:

```bash
cd frontend
npm install
npm run dev
```

The frontend will start at `http://localhost:5173` and automatically proxy `/api` requests to the backend.

### 5. Open the app

Go to [http://localhost:5173](http://localhost:5173) in your browser.

---

## How the GROQ API key is protected

- `appsettings.json` has an **empty** `ApiKey` field — the key is never stored there
- The backend reads `GROQ_API_KEY` from environment variables at runtime
- `appsettings.Development.json` is listed in `.gitignore` so local overrides are never pushed
- `.env` files are also listed in `.gitignore`

---

## API Reference

### `POST /api/analyze`

Analyzes resume text and returns an ATS score.

**Request body:**
```json
{
  "text": "Your resume text here..."
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "role": "Full-Stack Software Engineer",
    "level": "Junior",
    "score": 79,
    "techStack": ["React", "C#", ".NET", "SQL Server"],
    "strengths": ["Quantified achievements with real metrics"],
    "weaknesses": ["Missing CI/CD keywords"],
    "missingKeywords": ["Docker", "Kubernetes", "CI/CD"],
    "suggestions": ["Add measurable outcomes to each bullet point"],
    "advice": "Focus on demonstrating impact through numbers."
  }
}
```