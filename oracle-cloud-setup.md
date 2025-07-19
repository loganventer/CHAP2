# Oracle Cloud Setup Guide

## 1. Create Virtual Cloud Network (VCN)

1. **Navigate to Networking → Virtual Cloud Networks**
2. **Click "Create VCN"**
3. **Configure VCN:**
   - Name: `chap2-vcn`
   - CIDR Block: `10.0.0.0/16`
   - DNS Resolution: Enabled
   - DNS Hostnames: Enabled

## 2. Create Subnet

1. **In your VCN, click "Create Subnet"**
2. **Configure Subnet:**
   - Name: `chap2-subnet`
   - CIDR Block: `10.0.1.0/24`
   - Route Table: Default Route Table
   - Security List: Default Security List

## 3. Create Compute Instance

1. **Navigate to Compute → Instances**
2. **Click "Create Instance"**
3. **Configure Instance:**
   - Name: `chap2-server`
   - Image: Oracle Linux 9
   - Shape: VM.Standard.A1.Flex (ARM-based, 1 OCPU, 6 GB memory)
   - Network: Your VCN
   - Subnet: Your subnet
   - Public IP: Yes

## 4. Configure Security List

1. **Navigate to Networking → Virtual Cloud Networks**
2. **Click on your VCN → Security Lists**
3. **Click on Default Security List**
4. **Add Ingress Rules:**

### SSH Access:
- Source: `0.0.0.0/0`
- Port: `22`
- Protocol: `TCP`

### HTTP Access:
- Source: `0.0.0.0/0`
- Port: `80`
- Protocol: `TCP`

### HTTPS Access:
- Source: `0.0.0.0/0`
- Port: `443`
- Protocol: `TCP`

### CHAP2 API Access:
- Source: `0.0.0.0/0`
- Port: `5000`
- Protocol: `TCP`

### CHAP2 Web Portal Access:
- Source: `0.0.0.0/0`
- Port: `5001`
- Protocol: `TCP`

## 5. Connect to Your Instance

```bash
# SSH to your instance
ssh opc@your-instance-public-ip

# Update system
sudo dnf update -y
sudo dnf upgrade -y
``` 