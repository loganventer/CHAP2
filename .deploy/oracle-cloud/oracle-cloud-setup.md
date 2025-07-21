# Oracle Cloud Setup Guide

## Option 1: Automated Setup with Cloud-Init (Recommended)

This is the easiest way to set up CHAP2 on Oracle Cloud. The cloud-init configuration will automatically install Docker, clone the repository, and deploy the application during instance creation.

### 1. Create Virtual Cloud Network (VCN)

1. **Navigate to Networking → Virtual Cloud Networks**
2. **Click "Create VCN"**
3. **Configure VCN:**
   - Name: `chap2-vcn`
   - CIDR Block: `10.0.0.0/16`
   - DNS Resolution: Enabled
   - DNS Hostnames: Enabled

### 2. Create Subnet

1. **In your VCN, click "Create Subnet"**
2. **Configure Subnet:**
   - Name: `chap2-subnet`
   - CIDR Block: `10.0.1.0/24`
   - Route Table: Default Route Table
   - Security List: Default Security List

### 3. Configure Security List

1. **Navigate to Networking → Virtual Cloud Networks**
2. **Click on your VCN → Security Lists**
3. **Click on Default Security List**
4. **Add Ingress Rules:**

#### SSH Access:
- Source: `0.0.0.0/0`
- Port: `22`
- Protocol: `TCP`

#### HTTP Access:
- Source: `0.0.0.0/0`
- Port: `80`
- Protocol: `TCP`

#### HTTPS Access:
- Source: `0.0.0.0/0`
- Port: `443`
- Protocol: `TCP`

#### CHAP2 API Access:
- Source: `0.0.0.0/0`
- Port: `5000`
- Protocol: `TCP`

#### CHAP2 Web Portal Access:
- Source: `0.0.0.0/0`
- Port: `5001`
- Protocol: `TCP`

### 4. Create Compute Instance with Cloud-Init

1. **Navigate to Compute → Instances**
2. **Click "Create Instance"**
3. **Configure Instance:**
   - Name: `chap2-server`
   - Image: Oracle Linux 9
   - Shape: VM.Standard.A1.Flex (ARM-based, 1 OCPU, 6 GB memory)
   - Network: Your VCN
   - Subnet: Your subnet
   - Public IP: Yes

4. **Advanced Options → Cloud-Init:**
   - Copy the contents of `oracle-cloud-init.yml` into the cloud-init configuration field
   - This will automatically install Docker, clone the repository, and deploy CHAP2

5. **Click "Create"**

### 5. Wait for Setup to Complete

The cloud-init script will automatically:
- ✅ Install Docker and Docker Compose
- ✅ Configure firewall rules
- ✅ Clone the CHAP2 repository
- ✅ Set up auto-startup service
- ✅ Deploy the application
- ✅ Configure monitoring and log rotation

**Setup typically takes 5-10 minutes.** You can monitor progress by SSH'ing into the instance and checking:
```bash
# Check cloud-init logs
tail -f /var/log/chap2-cloud-init.log

# Check if setup is complete
ls -la /home/opc/chap2-setup-complete.txt

# Run welcome script to see status
/home/opc/welcome-chap2.sh
```

### 6. Access Your Application

Once setup is complete, your CHAP2 application will be available at:
- **Web Portal**: `http://your-instance-public-ip:5001`
- **API**: `http://your-instance-public-ip:5000`
- **API Health**: `http://your-instance-public-ip:5000/api/health/ping`

---

## Option 2: Manual Setup

If you prefer to set up manually or need to troubleshoot, follow these steps:

### 1. Create Virtual Cloud Network (VCN)

1. **Navigate to Networking → Virtual Cloud Networks**
2. **Click "Create VCN"**
3. **Configure VCN:**
   - Name: `chap2-vcn`
   - CIDR Block: `10.0.0.0/16`
   - DNS Resolution: Enabled
   - DNS Hostnames: Enabled

### 2. Create Subnet

1. **In your VCN, click "Create Subnet"**
2. **Configure Subnet:**
   - Name: `chap2-subnet`
   - CIDR Block: `10.0.1.0/24`
   - Route Table: Default Route Table
   - Security List: Default Security List

### 3. Create Compute Instance

1. **Navigate to Compute → Instances**
2. **Click "Create Instance"**
3. **Configure Instance:**
   - Name: `chap2-server`
   - Image: Oracle Linux 9
   - Shape: VM.Standard.A1.Flex (ARM-based, 1 OCPU, 6 GB memory)
   - Network: Your VCN
   - Subnet: Your subnet
   - Public IP: Yes

### 4. Configure Security List

1. **Navigate to Networking → Virtual Cloud Networks**
2. **Click on your VCN → Security Lists**
3. **Click on Default Security List**
4. **Add Ingress Rules:**

#### SSH Access:
- Source: `0.0.0.0/0`
- Port: `22`
- Protocol: `TCP`

#### HTTP Access:
- Source: `0.0.0.0/0`
- Port: `80`
- Protocol: `TCP`

#### HTTPS Access:
- Source: `0.0.0.0/0`
- Port: `443`
- Protocol: `TCP`

#### CHAP2 API Access:
- Source: `0.0.0.0/0`
- Port: `5000`
- Protocol: `TCP`

#### CHAP2 Web Portal Access:
- Source: `0.0.0.0/0`
- Port: `5001`
- Protocol: `TCP`

### 5. Connect to Your Instance

```bash
# SSH to your instance
ssh opc@your-instance-public-ip

# Update system
sudo dnf update -y
sudo dnf upgrade -y
```

### 6. Run the Setup Script

```bash
# Clone the repository
git clone https://github.com/loganventer/CHAP2.git
cd CHAP2

# Run the Oracle Linux setup script
chmod +x oracle-linux-setup.sh
./oracle-linux-setup.sh

# Deploy the application
chmod +x deploy-oracle-cloud.sh
./deploy-oracle-cloud.sh deploy
```

---

## Management Commands

Once your application is running, you can use these commands to manage it:

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

### Monitor System Resources
```bash
# Check disk usage
df -h

# Check memory usage
free -h

# Check Docker disk usage
docker system df
```

---

## Troubleshooting

### Application Not Starting
1. Check if Docker is running: `systemctl status docker`
2. Check container logs: `docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml logs`
3. Check startup script logs: `tail -f /var/log/chap2-startup.log`

### Port Access Issues
1. Verify firewall rules: `firewall-cmd --list-all`
2. Check if ports are open: `netstat -tlnp | grep :5000`

### Docker Issues
1. Restart Docker: `systemctl restart docker`
2. Check Docker logs: `journalctl -u docker`

### Auto-Startup Issues
1. Check service status: `systemctl status chap2-startup.service`
2. View service logs: `journalctl -u chap2-startup.service`

---

## Security Considerations

- The default setup allows access from anywhere (`0.0.0.0/0`)
- Consider restricting access to specific IP ranges for production use
- Regularly update the system and Docker images
- Monitor logs for suspicious activity
- Consider using HTTPS with a reverse proxy for production deployments 