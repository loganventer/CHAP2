# ☁️ CHAP2 Oracle Cloud Free Tier Deployment Guide

This guide provides step-by-step instructions for deploying the CHAP2 Musical Chorus Management System on Oracle Cloud Free Tier using Docker.

## 🎯 **Oracle Cloud Free Tier Benefits**

### **Always Free Resources:**
- **2 AMD-based Compute VMs** (1/8 OCPU, 1 GB memory each)
- **4 ARM-based Compute VMs** (1/24 OCPU, 6 GB memory each)
- **200 GB total storage**
- **10 TB data transfer**
- **Always Free** (no expiration)

### **Recommended Configuration:**
- **VM.Standard.A1.Flex** (ARM-based)
- **1 OCPU, 6 GB memory**
- **Oracle Linux 9**
- **Public IP address**

---

## 🚀 **Quick Start Deployment**

### **Step 1: Create Oracle Cloud Account**

1. **Sign up** at [Oracle Cloud Free Tier](https://www.oracle.com/cloud/free/)
2. **Verify your email** and complete registration
3. **Add payment method** (required, but won't be charged for free tier)
4. **Choose your home region** (closest to your location)

### **Step 2: Create Oracle Linux VM**

Follow the setup guide in `oracle-cloud-setup.md` or use the Oracle Cloud Console:

1. **Create Virtual Cloud Network (VCN)**
2. **Create Subnet**
3. **Create Compute Instance**
4. **Configure Security List**

### **Step 3: Connect to Your Instance**

```bash
# SSH to your instance
ssh opc@your-instance-public-ip

# Update system
sudo dnf update -y
sudo dnf upgrade -y
```

### **Step 4: Install Docker**

```bash
# Download and run the setup script
wget https://raw.githubusercontent.com/your-repo/CHAP2/main/oracle-linux-setup.sh
chmod +x oracle-linux-setup.sh
./oracle-linux-setup.sh

# Log out and back in for group changes
exit
ssh opc@your-instance-public-ip
```

### **Step 5: Deploy CHAP2**

```bash
# Clone the repository
git clone https://github.com/your-repo/CHAP2.git
cd CHAP2

# Make deployment script executable
chmod +x deploy-oracle-cloud.sh

# Deploy the application
./deploy-oracle-cloud.sh deploy
```

### **Step 6: Access Your Application**

After deployment, you can access:
- **Web Portal**: `http://your-instance-public-ip:5001`
- **API**: `http://your-instance-public-ip:5000`
- **API Health**: `http://your-instance-public-ip:5000/api/health/ping`

---

## 📋 **Available Commands**

### **Deployment Script**
```bash
./deploy-oracle-cloud.sh deploy    # Build and start services
./deploy-oracle-cloud.sh stop      # Stop all services
./deploy-oracle-cloud.sh logs      # Show container logs
./deploy-oracle-cloud.sh status    # Show service status
./deploy-oracle-cloud.sh help      # Show help
```

### **Docker Compose Commands**
```bash
# Build and start
docker-compose -f docker-compose.oracle-cloud.yml up --build -d

# View logs
docker-compose -f docker-compose.oracle-cloud.yml logs -f

# Stop services
docker-compose -f docker-compose.oracle-cloud.yml down

# Restart services
docker-compose -f docker-compose.oracle-cloud.yml restart
```

---

## 🏗️ **Architecture**

### **Services**
- **chap2-api-oracle**: CHAP2.Chorus.Api service
  - Port: 5000 (HTTP), 7000 (HTTPS)
  - Health check: `/api/health/ping`
  - Data volume: `./data/chorus`
  - Resource limits: 512MB memory, 0.5 CPU

- **chap2-web-oracle**: CHAP2.WebPortal service
  - Port: 5001 (HTTP), 7001 (HTTPS)
  - Depends on: chap2-api-oracle
  - Environment: `ApiBaseUrl=http://chap2-api`
  - Resource limits: 512MB memory, 0.5 CPU

### **Network**
- **chap2-network**: Bridge network for inter-service communication

### **Volumes**
- **chorus-data**: Persistent storage for chorus files

---

## 🔧 **Configuration**

### **Environment Variables**

#### **API Service**
```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:80;https://+:443
```

#### **Web Portal Service**
```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:80;https://+:443
ApiBaseUrl: http://chap2-api
```

### **Resource Limits**
- **Memory**: 512MB per container (fits within free tier limits)
- **CPU**: 0.5 CPU cores per container
- **Storage**: Uses local storage (included in free tier)

### **Port Mapping**
- API: `5000:80` (HTTP), `7000:443` (HTTPS)
- Web Portal: `5001:80` (HTTP), `7001:443` (HTTPS)

---

## 🧪 **Testing the Deployment**

### **1. Health Check**
```bash
curl http://your-instance-public-ip:5000/api/health/ping
```

### **2. API Endpoints**
```bash
# Get all choruses
curl http://your-instance-public-ip:5000/api/choruses

# Search choruses
curl "http://your-instance-public-ip:5000/api/choruses/search?q=grace"

# Create a new chorus
curl -X POST http://your-instance-public-ip:5000/api/choruses \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Chorus","chorusText":"Test lyrics","key":1,"type":1,"timeSignature":1}'
```

### **3. Web Portal**
- Open `http://your-instance-public-ip:5001` in your browser
- Test search functionality
- Test chorus creation and editing

---

## 🔍 **Troubleshooting**

### **Common Issues**

#### **1. Build Failures**
```bash
# Check available memory
free -h

# Check disk space
df -h

# Clean Docker cache
docker system prune -a

# Rebuild without cache
docker-compose -f docker-compose.oracle-cloud.yml build --no-cache
```

#### **2. Out of Memory**
```bash
# Check memory usage
docker stats

# Check system memory
free -h

# Restart Docker
sudo systemctl restart docker
```

#### **3. Network Issues**
```bash
# Check network connectivity
ping google.com

# Check if ports are open
netstat -tulpn | grep :5000
netstat -tulpn | grep :5001

# Check firewall
sudo firewall-cmd --list-all
```

#### **4. Security List Issues**
```bash
# Verify security list rules in Oracle Cloud Console
# Ensure ports 22, 80, 443, 5000, 5001 are open
```

### **Debug Commands**

#### **View Container Logs**
```bash
# All services
docker-compose -f docker-compose.oracle-cloud.yml logs -f

# Specific service
docker-compose -f docker-compose.oracle-cloud.yml logs -f chap2-api-oracle
docker-compose -f docker-compose.oracle-cloud.yml logs -f chap2-web-oracle
```

#### **Access Container Shell**
```bash
# API container
docker exec -it chap2-api-oracle /bin/bash

# Web Portal container
docker exec -it chap2-web-oracle /bin/bash
```

#### **Check Container Resources**
```bash
# Resource usage
docker stats

# Container details
docker inspect chap2-api-oracle
docker inspect chap2-web-oracle
```

---

## 🔒 **Security Considerations**

### **Production Deployment**
1. **Use HTTPS** in production
2. **Configure firewall** rules
3. **Regular updates** for Oracle Linux
4. **Backup data** regularly
5. **Monitor resource usage**

### **Security Best Practices**
```bash
# Update Oracle Linux
sudo dnf update -y

# Configure firewall
sudo firewall-cmd --permanent --add-port=22/tcp
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --permanent --add-port=5001/tcp
sudo firewall-cmd --reload

# Change default password
passwd
```

---

## 📊 **Monitoring**

### **System Monitoring**
```bash
# Check CPU usage
htop

# Check memory usage
free -h

# Check disk usage
df -h

# Check network usage
iftop
```

### **Application Monitoring**
```bash
# Check container status
docker-compose -f docker-compose.oracle-cloud.yml ps

# Check application logs
docker-compose -f docker-compose.oracle-cloud.yml logs -f

# Monitor resource usage
docker stats
```

---

## 🔄 **Updates and Maintenance**

### **Updating the Application**
```bash
# Pull latest changes
git pull

# Rebuild and restart
./deploy-oracle-cloud.sh deploy
```

### **Backup and Restore**
```bash
# Backup chorus data
tar -czf chorus-backup-$(date +%Y%m%d).tar.gz ./data/chorus/

# Restore chorus data
tar -xzf chorus-backup-20240101.tar.gz
```

### **System Maintenance**
```bash
# Update system packages
sudo dnf update -y

# Clean Docker
docker system prune -a

# Check disk space
df -h
```

---

## 🌐 **Domain and SSL**

### **Custom Domain (Optional)**
1. **Purchase a domain** (e.g., from Namecheap, GoDaddy)
2. **Point DNS** to your Oracle Cloud instance IP
3. **Configure reverse proxy** with Nginx
4. **Add SSL certificate** with Let's Encrypt

### **SSL Setup Example**
```bash
# Install Nginx
sudo dnf install -y nginx

# Configure Nginx as reverse proxy
sudo nano /etc/nginx/conf.d/chap2.conf

# Install Certbot
sudo dnf install -y certbot python3-certbot-nginx

# Get SSL certificate
sudo certbot --nginx -d your-domain.com
```

---

## 📱 **Mobile Access**

### **Public Access**
- **Anywhere access**: Your application is publicly accessible
- **Mobile friendly**: Responsive web design works on all devices
- **No VPN required**: Direct access via public IP

### **Performance**
- **Low latency**: Oracle Cloud's global network
- **High availability**: 99.95% uptime SLA
- **Scalable**: Can upgrade resources as needed

---

## 💰 **Cost Optimization**

### **Free Tier Limits**
- **4 ARM-based VMs** (1/24 OCPU, 6 GB memory each)
- **200 GB total storage**
- **10 TB data transfer**

### **Resource Usage**
- **CHAP2 API**: ~256MB memory, 0.25 CPU
- **CHAP2 Web**: ~256MB memory, 0.25 CPU
- **Total**: ~512MB memory, 0.5 CPU (well within limits)

### **Cost Monitoring**
- **Oracle Cloud Console**: Monitor resource usage
- **Billing alerts**: Set up spending limits
- **Resource optimization**: Use only what you need

---

## 🎯 **Benefits of Oracle Cloud**

### **Free Tier Advantages**
✅ **Always Free** - No expiration  
✅ **High Performance** - Oracle's enterprise infrastructure  
✅ **Global Network** - Low latency worldwide  
✅ **Security** - Enterprise-grade security  
✅ **Reliability** - 99.95% uptime SLA  
✅ **Scalability** - Easy to upgrade resources  
✅ **Support** - Oracle's enterprise support  

### **Technical Benefits**
✅ **ARM64 Support** - Native ARM64 containers  
✅ **Docker Native** - Full Docker support  
✅ **Network Performance** - High bandwidth  
✅ **Storage Performance** - Fast SSD storage  
✅ **Security Groups** - Fine-grained network control  

---

## 🤝 **Support and Community**

### **Oracle Cloud Support**
- **Free Tier Support**: Community forums
- **Paid Support**: Oracle support plans
- **Documentation**: Comprehensive guides

### **CHAP2 Community**
- **GitHub Issues**: Report bugs and request features
- **Discussions**: Community support
- **Documentation**: Comprehensive guides

---

## 📄 **License**

This Oracle Cloud deployment is part of the CHAP2 system. 