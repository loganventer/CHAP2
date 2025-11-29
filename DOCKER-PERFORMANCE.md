# Docker Deployment Performance Guide

## 🚀 Quick Start

For **normal development** use:
```bash
./start-chap2.sh
```

For **forced rebuild** (only when needed):
```bash
./rebuild-chap2.sh
```

---

## ⚡ Performance Improvements Applied

### 1. Added `.dockerignore` File
**Impact:** Reduces build context from ~89MB to ~10MB
**Speed up:** ~7 minutes faster

The `.dockerignore` file now excludes:
- `bin/` and `obj/` directories (build artifacts)
- `node_modules/` (npm packages)
- `.git/` (version control)
- IDE files and logs
- Test results

### 2. Removed `--build` Flag
**Impact:** Docker only rebuilds when Dockerfiles or source code changes
**Speed up:** 5-10 minutes on unchanged deploys

The startup script now uses:
- `docker-compose up -d` (smart rebuild)

Instead of:
- `docker-compose up -d --build` (always rebuild)

---

## 📊 Expected Performance

| Scenario | Before | After |
|----------|--------|-------|
| **First Deploy** | 10-15 min | 10-15 min (no change) |
| **Restart (no changes)** | 10-15 min | **30-60 sec** ✅ |
| **Code change (JS/CSS only)** | 10-15 min | **2-3 min** ✅ |
| **Code change (.NET)** | 10-15 min | **3-5 min** ✅ |

---

## 🔧 When to Force Rebuild

Use `./rebuild-chap2.sh` when:
- ✅ You've updated NuGet packages
- ✅ You've updated Python requirements
- ✅ You've changed Dockerfiles
- ✅ You suspect Docker cache issues
- ✅ First deployment on a new machine

**Don't** use it for:
- ❌ Regular code changes (JS, CSS, C#)
- ❌ Testing small updates
- ❌ Daily development work

---

## 🐛 Troubleshooting

### "Container won't start after code change"
```bash
# Force rebuild just that service
cd .deploy/linux-mac
docker-compose build chap2-webportal
docker-compose up -d chap2-webportal
```

### "I want to clear everything and start fresh"
```bash
cd .deploy/linux-mac
docker-compose down -v  # Removes volumes too
docker system prune -a  # Removes all unused images
./rebuild-chap2.sh     # Fresh rebuild
```

### "Build context is still slow"
```bash
# Check what's being copied
cd /Users/logan/Documents/dev/CHAP2
find . -type f ! -path './.git/*' | wc -l

# If > 5000 files, check .dockerignore is working:
docker build --no-cache -f langchain_search_service/Dockerfile.api.simple -t test-context . 2>&1 | grep "transferring context"
```

---

## 📝 Manual Commands

### Start services (fast, reuses images)
```bash
cd .deploy/linux-mac
docker-compose up -d
```

### Rebuild specific service
```bash
cd .deploy/linux-mac
docker-compose build langchain-service
docker-compose up -d langchain-service
```

### View logs
```bash
cd .deploy/linux-mac
docker-compose logs -f chap2-webportal
```

### Stop all services
```bash
cd .deploy/linux-mac
docker-compose down
```

### Check service status
```bash
cd .deploy/linux-mac
docker-compose ps
```

---

## 🎯 Best Practices

1. **Use start-chap2.sh for daily work** - It's optimized for speed
2. **Only rebuild when necessary** - Docker's cache is your friend
3. **Keep .dockerignore updated** - Don't copy unnecessary files
4. **Use Docker BuildKit** - Automatically enabled in newer Docker versions
5. **Monitor build times** - If it's consistently slow, investigate

---

## 🔍 Advanced Optimization (Optional)

### Enable BuildKit for parallel builds
Add to `~/.docker/config.json`:
```json
{
  "features": {
    "buildkit": true
  }
}
```

### Use multi-stage build caching
Already implemented in Dockerfiles! This helps:
- Restore NuGet packages in one layer
- Build in another layer
- Only rebuild changed layers

---

## 💡 Tips

- **First build is always slow** - Docker needs to download base images and dependencies
- **Subsequent builds are fast** - Thanks to layer caching
- **Changing a Dockerfile forces rebuild** - But only for that service
- **Code changes only rebuild affected layers** - Thanks to multi-stage builds

---

Last updated: 2025-11-29
