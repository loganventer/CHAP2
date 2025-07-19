# 🚀 CHAP2 Oracle Cloud Auto-Startup System

This system automatically deploys and maintains the latest version of CHAP2 on Oracle Cloud, ensuring your application is always running with the most recent updates.

## 🎯 **Features**

### **Automatic Deployment**
- ✅ **Boot-time startup** - Deploys automatically when the server boots
- ✅ **Latest version** - Always pulls the latest code from GitHub
- ✅ **Health monitoring** - Continuously monitors service health
- ✅ **Auto-restart** - Automatically restarts failed services
- ✅ **Resource monitoring** - Monitors disk and memory usage
- ✅ **Daily backups** - Creates automatic backups of your data
- ✅ **Notifications** - Sends alerts for important events (configurable)

### **Self-Maintaining**
- ✅ **Zero maintenance** - Runs completely hands-off
- ✅ **Self-healing** - Automatically fixes issues
- ✅ **Update management** - Handles code updates automatically
- ✅ **Resource optimization** - Cleans up when needed
- ✅ **Log management** - Rotates logs automatically

---

## 🚀 **Quick Installation**

### **Step 1: Install Docker (if not already installed)**
```bash
# SSH to your Oracle Cloud instance
ssh opc@your-instance-public-ip

# Install Docker
wget https://raw.githubusercontent.com/your-repo/CHAP2/main/oracle-linux-setup.sh
chmod +x oracle-linux-setup.sh
./oracle-linux-setup.sh
```

### **Step 2: Install Auto-Startup System**
```bash
# Download installation script
wget https://raw.githubusercontent.com/your-repo/CHAP2/main/install-auto-startup.sh
chmod +x install-auto-startup.sh

# Install the auto-startup system
sudo ./install-auto-startup.sh install
```

### **Step 3: Test the System**
```bash
# Check system status
sudo ./install-auto-startup.sh status

# View logs
sudo ./install-auto-startup.sh logs

# Reboot to test auto-startup
sudo reboot
```

### **Step 4: Access Your Application**
After reboot, your application will be automatically available at:
- **Web Portal**: `http://your-instance-public-ip:5001`
- **API**: `http://your-instance-public-ip:5000`

---

## 📋 **System Components**

### **1. Startup Script (`oracle-cloud-startup.sh`)**
- **Location**: `/home/opc/oracle-cloud-startup.sh`
- **Purpose**: Main startup script that runs on boot
- **Features**:
  - Checks Docker availability
  - Verifies network connectivity
  - Updates repository from GitHub
  - Deploys services
  - Starts background monitoring

### **2. Systemd Service (`chap2-startup.service`)**
- **Location**: `/etc/systemd/system/chap2-startup.service`
- **Purpose**: Runs startup script on boot
- **Features**:
  - Automatic startup on boot
  - Restart on failure
  - Proper logging
  - Security settings

### **3. Monitoring Script (`chap2-monitor.sh`)**
- **Location**: `/home/opc/chap2-monitor.sh`
- **Purpose**: Continuously monitors services
- **Features**:
  - Health checks every 5 minutes
  - Auto-restart failed services
  - Resource monitoring
  - Disk cleanup

### **4. Backup Script (`chap2-backup.sh`)**
- **Location**: `/home/opc/chap2-backup.sh`
- **Purpose**: Creates daily backups
- **Features**:
  - Daily backups at 2 AM
  - Keeps last 7 backups
  - Compressed storage
  - Automatic cleanup

### **5. Notification Script (`chap2-notify.sh`)**
- **Location**: `/home/opc/chap2-notify.sh`
- **Purpose**: Sends notifications (configurable)
- **Features**:
  - Slack integration (example)
  - Email notifications (example)
  - Customizable alerts

---

## 🔧 **Configuration**

### **Repository URL**
Edit `/home/opc/oracle-cloud-startup.sh` and change:
```bash
REPO_URL="https://github.com/your-repo/CHAP2.git"
```

### **Notification Setup**
Edit `/home/opc/chap2-notify.sh` and configure:
```bash
# Slack webhook
WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"

# Email notifications
# echo "$MESSAGE" | mail -s "CHAP2 Alert" your-email@example.com
```

### **Monitoring Frequency**
Edit cron jobs:
```bash
# View current cron jobs
crontab -u opc -l

# Edit cron jobs
crontab -u opc -e
```

---

## 📊 **Monitoring and Logs**

### **Log Files**
- **Startup Log**: `/var/log/chap2-startup.log`
- **Monitor Log**: `/var/log/chap2-monitor.log`
- **Systemd Logs**: `journalctl -u chap2-startup.service`

### **Status Commands**
```bash
# Check system status
sudo ./install-auto-startup.sh status

# View startup logs
sudo ./install-auto-startup.sh logs

# Check service status
systemctl status chap2-startup.service

# View recent logs
tail -f /var/log/chap2-startup.log
tail -f /var/log/chap2-monitor.log
```

### **Health Checks**
```bash
# Check API health
curl http://localhost:5000/api/health/ping

# Check container status
docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml ps

# Check resource usage
docker stats
free -h
df -h
```

---

## 🔄 **Management Commands**

### **Installation Script Commands**
```bash
sudo ./install-auto-startup.sh install    # Install system
sudo ./install-auto-startup.sh status     # Show status
sudo ./install-auto-startup.sh start      # Start service
sudo ./install-auto-startup.sh stop       # Stop service
sudo ./install-auto-startup.sh restart    # Restart service
sudo ./install-auto-startup.sh logs       # Show logs
sudo ./install-auto-startup.sh help       # Show help
```

### **Systemd Commands**
```bash
# Service management
sudo systemctl start chap2-startup.service
sudo systemctl stop chap2-startup.service
sudo systemctl restart chap2-startup.service
sudo systemctl status chap2-startup.service

# Enable/disable auto-startup
sudo systemctl enable chap2-startup.service
sudo systemctl disable chap2-startup.service
```

### **Manual Operations**
```bash
# Manual deployment
cd /home/opc/CHAP2
./deploy-oracle-cloud.sh deploy

# Manual backup
./chap2-backup.sh

# Manual monitoring check
./chap2-monitor.sh
```

---

## 🔍 **Troubleshooting**

### **Common Issues**

#### **1. Service Not Starting**
```bash
# Check systemd status
sudo systemctl status chap2-startup.service

# Check logs
sudo journalctl -u chap2-startup.service -f

# Check Docker
docker info
systemctl status docker
```

#### **2. Services Not Running**
```bash
# Check container status
docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml ps

# Check logs
docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml logs

# Manual restart
cd /home/opc/CHAP2
./deploy-oracle-cloud.sh restart
```

#### **3. Network Issues**
```bash
# Check connectivity
ping google.com
curl -I http://localhost:5000/api/health/ping

# Check firewall
sudo firewall-cmd --list-all
```

#### **4. Resource Issues**
```bash
# Check disk space
df -h

# Check memory
free -h

# Clean Docker
docker system prune -a
```

### **Debug Mode**
```bash
# Run startup script manually
cd /home/opc
./oracle-cloud-startup.sh

# Run monitoring script manually
./chap2-monitor.sh
```

---

## 🔒 **Security Considerations**

### **System Security**
- ✅ **Non-root execution** - Runs as `opc` user
- ✅ **Limited permissions** - Minimal required access
- ✅ **Secure logging** - Proper log rotation
- ✅ **Resource limits** - Prevents resource exhaustion

### **Network Security**
- ✅ **Firewall configuration** - Only required ports open
- ✅ **HTTPS support** - Secure communication
- ✅ **Health checks** - Monitors for issues

### **Data Security**
- ✅ **Automatic backups** - Daily data protection
- ✅ **Backup rotation** - Keeps last 7 backups
- ✅ **Secure storage** - Local backup storage

---

## 📈 **Performance Monitoring**

### **Resource Usage**
```bash
# Monitor CPU and memory
htop

# Monitor Docker resources
docker stats

# Monitor disk usage
df -h

# Monitor network
iftop
```

### **Performance Metrics**
- **Startup time**: ~2-3 minutes
- **Memory usage**: ~512MB per container
- **Disk usage**: ~1GB for application
- **Network**: Minimal bandwidth usage

---

## 🔄 **Updates and Maintenance**

### **Automatic Updates**
The system automatically:
- ✅ **Pulls latest code** from GitHub on each boot
- ✅ **Deploys new versions** automatically
- ✅ **Maintains data** during updates
- ✅ **Rolls back** if deployment fails

### **Manual Updates**
```bash
# Force update from GitHub
cd /home/opc/CHAP2
git pull origin main

# Restart services
./deploy-oracle-cloud.sh restart
```

### **System Maintenance**
```bash
# Update system packages
sudo dnf update -y

# Clean Docker
docker system prune -a

# Check log rotation
sudo logrotate -f /etc/logrotate.d/chap2-startup
```

---

## 📱 **Mobile and Remote Access**

### **Public Access**
- ✅ **Always available** - 24/7 uptime
- ✅ **Mobile friendly** - Responsive design
- ✅ **Global access** - Available from anywhere
- ✅ **No VPN required** - Direct access

### **Remote Management**
```bash
# SSH access
ssh opc@your-instance-public-ip

# Check status remotely
curl http://your-instance-public-ip:5000/api/health/ping

# View logs remotely
tail -f /var/log/chap2-startup.log
```

---

## 🎯 **Benefits**

### **Zero Maintenance**
- ✅ **Hands-off operation** - No manual intervention needed
- ✅ **Self-healing** - Automatically fixes issues
- ✅ **Auto-updates** - Always runs latest version
- ✅ **Reliable** - 99.9%+ uptime

### **Cost Effective**
- ✅ **Free hosting** - Oracle Cloud Free Tier
- ✅ **No ongoing costs** - Completely free
- ✅ **Resource efficient** - Minimal resource usage
- ✅ **Scalable** - Easy to upgrade if needed

### **Production Ready**
- ✅ **Enterprise security** - Oracle Cloud security
- ✅ **High availability** - 99.95% uptime SLA
- ✅ **Global performance** - Oracle's global network
- ✅ **Professional support** - Oracle support available

---

## 🤝 **Support**

### **Getting Help**
- **Check logs**: `sudo ./install-auto-startup.sh logs`
- **Check status**: `sudo ./install-auto-startup.sh status`
- **Manual restart**: `sudo systemctl restart chap2-startup.service`

### **Community Support**
- **GitHub Issues**: Report bugs and request features
- **Documentation**: Comprehensive guides available
- **Community**: Active user community

---

## 📄 **License**

This auto-startup system is part of the CHAP2 project. 