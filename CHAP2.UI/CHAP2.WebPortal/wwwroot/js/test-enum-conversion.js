// Test script for enum conversion
console.log('🧪 Testing enum conversion...');

// Test the enum conversion functions
function testEnumConversion() {
    // Test key conversion
    const testKeys = [0, 1, 19, 15, 2];
    console.log('Testing key conversion:');
    testKeys.forEach(key => {
        const display = getKeyDisplay(key);
        console.log(`Key ${key} -> ${display}`);
    });
    
    // Test type conversion
    const testTypes = [0, 1, 2];
    console.log('\nTesting type conversion:');
    testTypes.forEach(type => {
        const display = getTypeDisplay(type);
        console.log(`Type ${type} -> ${display}`);
    });
    
    // Test time signature conversion
    const testTimes = [0, 1, 2, 3];
    console.log('\nTesting time signature conversion:');
    testTimes.forEach(time => {
        const display = getTimeSignatureDisplay(time);
        console.log(`Time ${time} -> ${display}`);
    });
}

// Enum conversion functions (copied from search-v2.js)
const MusicalKeys = {
    0: 'Not Set', 1: 'C', 2: 'C#', 3: 'D', 4: 'D#', 5: 'E', 6: 'F', 7: 'F#', 8: 'G', 9: 'G#', 10: 'A', 11: 'A#', 12: 'B',
    13: 'C♭', 14: 'D♭', 15: 'E♭', 16: 'F♭', 17: 'G♭', 18: 'A♭', 19: 'B♭'
};

const ChorusTypes = {
    0: 'Not Set', 1: 'Praise', 2: 'Worship'
};

const TimeSignatures = {
    0: 'Not Set', 1: '4/4', 2: '3/4', 3: '6/8', 4: '2/4', 5: '4/8', 6: '3/8', 7: '2/2',
    8: '5/4', 9: '6/4', 10: '9/8', 11: '12/8', 12: '7/4', 13: '8/4',
    14: '5/8', 15: '7/8', 16: '8/8', 17: '2/16', 18: '3/16', 19: '4/16',
    20: '5/16', 21: '6/16', 22: '7/16', 23: '8/16', 24: '9/16', 25: '12/16'
};

function getKeyDisplay(keyValue) {
    const numValue = parseInt(keyValue);
    return MusicalKeys[numValue] || 'Unknown';
}

function getTypeDisplay(typeValue) {
    const numValue = parseInt(typeValue);
    return ChorusTypes[numValue] || 'Unknown';
}

function getTimeSignatureDisplay(timeValue) {
    const numValue = parseInt(timeValue);
    return TimeSignatures[numValue] || 'Unknown';
}

// Run the test
testEnumConversion();

console.log('✅ Enum conversion test completed!'); 