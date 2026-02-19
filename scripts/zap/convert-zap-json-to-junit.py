import json
import glob
import xml.etree.ElementTree as ET

for file in glob.glob("zap-reports/*.json"):
  with open(file) as f:
    data = json.load(f)

  testsuite = ET.Element("testsuite", name="ZAP Security Scan")

  total = 0
  failures = 0
  skipped = 0

  for site in data.get("site", []):
    site_name = site.get("name", "ZAP")

    for alert in site.get("alerts", []):
      total += 1

      alert_name = alert.get("alert", "Unknown Alert")
      severity = alert.get("riskdesc", "")
      description = alert.get("desc", "")

      testcase = ET.SubElement(
        testsuite,
        "testcase",
        classname=site_name,
        name=f"{alert_name} ({severity})"
      )

      if "High" in severity:
        failures += 1
        failure = ET.SubElement(
          testcase,
          "failure",
          message=severity
        )
        failure.text = description

      elif "Medium" in severity:
        skipped += 1
        skip = ET.SubElement(
          testcase,
          "skipped",
          message=severity
        )
        skip.text = description

    testsuite.set("tests", str(total))
    testsuite.set("failures", str(failures))
    testsuite.set("skipped", str(skipped))

    xml_file = file.replace(".json", ".xml")

    tree = ET.ElementTree(testsuite)
    with open(xml_file, "wb") as f:
      tree.write(f, encoding="utf-8", xml_declaration=True)

    print(f"Created JUnit report: {xml_file}")
    print(f"Total: {total}, High: {failures}, Medium: {skipped}")
