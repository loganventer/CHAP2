// Debug CRUD functionality for CHAP2 Web Portal
class CrudDebugger {
    constructor() {
        this.apiBaseUrl = this.getApiBaseUrl();
        this.testResults = [];
    }

    getApiBaseUrl() {
        // Try to get the API base URL from the page
        const metaTag = document.querySelector('meta[name="api-base-url"]');
        if (metaTag) {
            return metaTag.getAttribute('content');
        }
        
        // Fallback to common URLs
        const possibleUrls = [
            'http://chap2-api:5001',
            'http://localhost:5001',
            'http://localhost:5050',
            window.location.origin.replace('5002', '5001'),
            window.location.origin.replace('5002', '5050')
        ];
        
        return possibleUrls[0];
    }

    async testConnectivity() {
        console.log('🔍 Testing API connectivity...');
        console.log('API Base URL:', this.apiBaseUrl);
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/health/ping`);
            const result = {
                success: response.ok,
                status: response.status,
                statusText: response.statusText,
                url: `${this.apiBaseUrl}/api/health/ping`
            };
            
            if (response.ok) {
                const data = await response.json();
                result.data = data;
                console.log('✅ API connectivity test passed:', result);
            } else {
                console.log('❌ API connectivity test failed:', result);
            }
            
            this.testResults.push({ test: 'connectivity', result });
            return result;
        } catch (error) {
            const result = {
                success: false,
                error: error.message,
                url: `${this.apiBaseUrl}/api/health/ping`
            };
            console.log('❌ API connectivity test failed with exception:', result);
            this.testResults.push({ test: 'connectivity', result });
            return result;
        }
    }

    async testCreate() {
        console.log('🔍 Testing CREATE operation...');
        
        const testChorus = {
            name: `Test Chorus ${Date.now()}`,
            chorusText: "This is a test chorus for debugging CRUD operations.\nSecond line for testing.",
            key: 1, // C
            type: 1, // Praise
            timeSignature: 1 // 4/4
        };
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/choruses`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(testChorus)
            });
            
            const result = {
                success: response.ok,
                status: response.status,
                statusText: response.statusText,
                url: `${this.apiBaseUrl}/api/choruses`
            };
            
            if (response.ok) {
                const data = await response.json();
                result.data = data;
                result.createdId = data.id;
                console.log('✅ CREATE test passed:', result);
            } else {
                const errorText = await response.text();
                result.error = errorText;
                console.log('❌ CREATE test failed:', result);
            }
            
            this.testResults.push({ test: 'create', result });
            return result;
        } catch (error) {
            const result = {
                success: false,
                error: error.message,
                url: `${this.apiBaseUrl}/api/choruses`
            };
            console.log('❌ CREATE test failed with exception:', result);
            this.testResults.push({ test: 'create', result });
            return result;
        }
    }

    async testRead(id) {
        console.log('🔍 Testing READ operation...');
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/choruses/${id}`);
            
            const result = {
                success: response.ok,
                status: response.status,
                statusText: response.statusText,
                url: `${this.apiBaseUrl}/api/choruses/${id}`
            };
            
            if (response.ok) {
                const data = await response.json();
                result.data = data;
                console.log('✅ READ test passed:', result);
            } else {
                const errorText = await response.text();
                result.error = errorText;
                console.log('❌ READ test failed:', result);
            }
            
            this.testResults.push({ test: 'read', result });
            return result;
        } catch (error) {
            const result = {
                success: false,
                error: error.message,
                url: `${this.apiBaseUrl}/api/choruses/${id}`
            };
            console.log('❌ READ test failed with exception:', result);
            this.testResults.push({ test: 'read', result });
            return result;
        }
    }

    async testUpdate(id) {
        console.log('🔍 Testing UPDATE operation...');
        
        const updateData = {
            name: `Updated Test Chorus ${Date.now()}`,
            chorusText: "This is an updated test chorus for debugging CRUD operations.\nUpdated second line.",
            key: 2, // C#
            type: 2, // Worship
            timeSignature: 2 // 3/4
        };
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/choruses/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updateData)
            });
            
            const result = {
                success: response.ok,
                status: response.status,
                statusText: response.statusText,
                url: `${this.apiBaseUrl}/api/choruses/${id}`
            };
            
            if (response.ok) {
                const data = await response.json();
                result.data = data;
                console.log('✅ UPDATE test passed:', result);
            } else {
                const errorText = await response.text();
                result.error = errorText;
                console.log('❌ UPDATE test failed:', result);
            }
            
            this.testResults.push({ test: 'update', result });
            return result;
        } catch (error) {
            const result = {
                success: false,
                error: error.message,
                url: `${this.apiBaseUrl}/api/choruses/${id}`
            };
            console.log('❌ UPDATE test failed with exception:', result);
            this.testResults.push({ test: 'update', result });
            return result;
        }
    }

    async testDelete(id) {
        console.log('🔍 Testing DELETE operation...');
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/choruses/${id}`, {
                method: 'DELETE'
            });
            
            const result = {
                success: response.ok,
                status: response.status,
                statusText: response.statusText,
                url: `${this.apiBaseUrl}/api/choruses/${id}`
            };
            
            if (response.ok) {
                console.log('✅ DELETE test passed:', result);
            } else {
                const errorText = await response.text();
                result.error = errorText;
                console.log('❌ DELETE test failed:', result);
            }
            
            this.testResults.push({ test: 'delete', result });
            return result;
        } catch (error) {
            const result = {
                success: false,
                error: error.message,
                url: `${this.apiBaseUrl}/api/choruses/${id}`
            };
            console.log('❌ DELETE test failed with exception:', result);
            this.testResults.push({ test: 'delete', result });
            return result;
        }
    }

    async runFullTest() {
        console.log('🚀 Starting full CRUD test...');
        console.log('API Base URL:', this.apiBaseUrl);
        
        // Test 1: Connectivity
        const connectivityResult = await this.testConnectivity();
        if (!connectivityResult.success) {
            console.log('❌ Connectivity test failed, stopping tests');
            return this.testResults;
        }
        
        // Test 2: Create
        const createResult = await this.testCreate();
        if (!createResult.success) {
            console.log('❌ Create test failed, stopping tests');
            return this.testResults;
        }
        
        const testId = createResult.createdId;
        
        // Test 3: Read
        await this.testRead(testId);
        
        // Test 4: Update
        await this.testUpdate(testId);
        
        // Test 5: Delete
        await this.testDelete(testId);
        
        console.log('🎉 Full CRUD test completed!');
        console.log('Test Results:', this.testResults);
        
        return this.testResults;
    }

    generateReport() {
        const report = {
            timestamp: new Date().toISOString(),
            apiBaseUrl: this.apiBaseUrl,
            tests: this.testResults,
            summary: {
                total: this.testResults.length,
                passed: this.testResults.filter(t => t.result.success).length,
                failed: this.testResults.filter(t => !t.result.success).length
            }
        };
        
        console.log('📊 CRUD Test Report:', report);
        return report;
    }
}

// Make it available globally
window.CrudDebugger = CrudDebugger;

// Auto-run test if debug mode is enabled
if (window.location.search.includes('debug=crud')) {
    document.addEventListener('DOMContentLoaded', async () => {
        console.log('🔧 CRUD Debug mode enabled');
        const crudDebugger = new CrudDebugger();
        await crudDebugger.runFullTest();
        crudDebugger.generateReport();
    });
} 