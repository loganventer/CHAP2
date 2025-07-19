# CHAP2 Oracle Cloud Automated Deployment

This repository includes automated deployment scripts for Oracle Cloud that will install Docker, clone the repository, and deploy CHAP2 automatically.

## 🚀 **Two Setup Options Available:**

### **Option 1: Cloud-Init (Recommended for New Instances)**
Use `oracle-cloud-init.yml` during instance creation for fully automated setup.

### **Option 2: Standalone Script (For Existing Instances)**
Use `oracle-cloud-init.sh` to set up existing instances manually.

---

## 📋 **Quick Start - Cloud-Init (New Instances)**

### 1. Create Oracle Cloud Instance

1. **Navigate to Oracle Cloud Console → Compute → Instances**
2. **Click "Create Instance"**
3. **Configure:**
   - Name: `chap2-server`
   - Image: Oracle Linux 9
   - Shape: VM.Standard.A1.Flex (1 OCPU, 6 GB memory)
   - Network: Your VCN with public subnet
   - Public IP: Yes

### 2. Add Cloud-Init Configuration

1. **In the "Advanced Options" section, expand "Cloud-Init"**
2. **Copy the entire contents of `oracle-cloud-init.yml` into the configuration field**
3. **Click "Create"**

### 3. Wait for Setup (5-10 minutes)

The cloud-init script will automatically:
- ✅ Install Docker and Docker Compose
- ✅ Configure firewall rules
- ✅ Clone the CHAP2 repository
- ✅ Set up auto-startup service
- ✅ Deploy the application
- ✅ Configure monitoring and log rotation

---

## 🔧 **Quick Start - Standalone Script (Existing Instances)**

### 1. SSH into Your Instance

```bash
ssh opc@your-instance-public-ip
```

### 2. Download and Run Setup Script

```bash
# Download the setup script
curl -O https://raw.githubusercontent.com/loganventer/CHAP2/main/oracle-cloud-init.sh

# Make it executable
chmod +x oracle-cloud-init.sh

# Run the setup
./oracle-cloud-init.sh
```

### 3. Wait for Setup (5-10 minutes)

The script will perform the same automated setup as cloud-init.

---

## 📊 **Monitoring Setup Progress**

SSH into your instance and run:
```bash
# Check cloud-init logs (if using cloud-init)
tail -f /var/log/chap2-cloud-init.log

# Check if setup is complete
ls -la /home/opc/chap2-setup-complete.txt

# Run welcome script to see status
/home/opc/welcome-chap2.sh
```

## 🌐 **Access Your Application**

Once setup is complete, your CHAP2 application will be available at:
- **Web Portal**: `http://your-instance-public-ip:5001`
- **API**: `http://your-instance-public-ip:5000`
- **API Health**: `http://your-instance-public-ip:5000/api/health/ping`

## 🔧 **Management Commands**

### View Application Status
```bash
# Check if services are running
docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml ps

# Check application health
curl http://localhost:5000/api/health/ping
```

### View Logs
```bash
# View container logs
docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml logs

# View startup script logs
tail -f /var/log/chap2-startup.log

# View cloud-init logs
tail -f /var/log/chap2-cloud-init.log
```

### Restart Application
```bash
# Restart all services
/home/opc/CHAP2/deploy-oracle-cloud.sh deploy

# Stop all services
/home/opc/CHAP2/deploy-oracle-cloud.sh stop
```

## 🔒 **Security Configuration**

Before creating your instance, make sure to configure the security list to allow:
- **SSH (Port 22)**: For remote access
- **HTTP (Port 80)**: For web traffic
- **HTTPS (Port 443)**: For secure web traffic
- **CHAP2 API (Port 5000)**: For API access
- **CHAP2 Web Portal (Port 5001)**: For web portal access

## 📁 **Files Included**

- `oracle-cloud-init.yml` - Cloud-init configuration for automated setup
- `oracle-cloud-init.sh` - Standalone setup script (alternative to cloud-init)
- `oracle-cloud-startup.sh` - Auto-startup script for system reboots
- `deploy-oracle-cloud.sh` - Deployment script for manual deployments
- `oracle-cloud-setup.md` - Detailed setup documentation

## 🎯 **When to Use Each Option**

### **Use Cloud-Init (`oracle-cloud-init.yml`) When:**
- ✅ Creating a new instance
- ✅ Want fully automated setup
- ✅ Have access to Oracle Cloud Console
- ✅ Want zero manual steps

### **Use Standalone Script (`oracle-cloud-init.sh`) When:**
- ✅ Working with existing instances
- ✅ Cloud-init failed or wasn't used
- ✅ Need to troubleshoot or re-setup
- ✅ Want to test the setup process
- ✅ Don't have cloud-init access

## 🔧 **Troubleshooting**

### Application Not Starting
1. Check if Docker is running: `systemctl status docker`
2. Check container logs: `docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml logs`
3. Check startup script logs: `tail -f /var/log/chap2-startup.log`

### Port Access Issues
1. Verify firewall rules: `firewall-cmd --list-all`
2. Check if ports are open: `netstat -tlnp | grep :5000`

### Auto-Startup Issues
1. Check service status: `systemctl status chap2-startup.service`
2. View service logs: `journalctl -u chap2-startup.service`

### Script Execution Issues
1. Check script permissions: `ls -la oracle-cloud-init.sh`
2. Run with verbose output: `bash -x oracle-cloud-init.sh`
3. Check system resources: `free -h && df -h`

## 📞 **Manual Setup (Alternative)**

If you prefer manual setup or need to troubleshoot, see `oracle-cloud-setup.md` for detailed manual setup instructions.

## 🌐 **Repository**

The CHAP2 application is available at: https://github.com/loganventer/CHAP2

## 🆘 **Support**

For issues or questions:
1. Check the logs mentioned above
2. Review the troubleshooting section
3. Check the detailed setup documentation in `oracle-cloud-setup.md`
4. Try both setup methods if one fails 