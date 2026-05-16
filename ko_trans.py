import xml.etree.ElementTree as ET
path = r"c:/Users/22737/source/repos/FactionGearModification/1.6/Languages/Korean/Keyed/FactionGearCustomizer.xml"
tree = ET.parse(path)
root = tree.getroot()
tr = {}
print("script created")