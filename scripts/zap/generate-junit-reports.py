import json
import glob
import os
import logging
import argparse
import xml.etree.ElementTree as ET


logging.basicConfig(
  level=logging.INFO,
  format="[%(levelname)s] %(message)s"
)



def load_json(file):
  try:
    logging.info(f"Processing {file}")
    with open(file, "r") as f:
      return json.load(f)
  except Exception as e:
    logging.error(f"Failed to read JSON file {file}: {e}")
    return None



def process_alert(testcase_parent, site_name, alert, counters):
  counters["total"] += 1

  alert_name = alert.get("alert", "Unknown Alert")
  severity = alert.get("riskdesc", "")
  description = alert.get("desc", "")

  testcase = ET.SubElement(
    testcase_parent,
    "testcase",
    classname=site_name,
    name=f"{alert_name} ({severity})"
  )

  if "High" in severity:
    counters["failures"] += 1
    failure = ET.SubElement(testcase, "failure", message=severity)
    failure.text = description

  elif "Medium" in severity:
    counters["skipped"] += 1
    skipped = ET.SubElement(testcase, "skipped", message=severity)
    skipped.text = description



def build_testsuite(data, file):
  testsuite = ET.Element("testsuite", name=f"ZAP Security Scan ({file})")
  counters = {"total": 0, "failures": 0, "skipped": 0}

  try:
    for site in data.get("site", []):
      site_name = site.get("name", "ZAP")
      for alert in site.get("alerts", []):
        process_alert(testsuite, site_name, alert, counters)
  except Exception as e:
    logging.error(f"Error processing alerts in {file}: {e}")

  testsuite.set("tests", str(counters["total"]))
  testsuite.set("failures", str(counters["failures"]))
  testsuite.set("skipped", str(counters["skipped"]))

  return testsuite, counters



def write_xml(output_file, testsuite):
  try:
    tree = ET.ElementTree(testsuite)
    with open(output_file, "wb") as f:
      tree.write(f, encoding="utf-8", xml_declaration=True)
  except Exception as e:
    logging.error(f"Failed to write XML file {output_file}: {e}")
    return False
  return True



def create_summary_file(summary_file: str, text: str):
  """
  Create summary file with the provided text in the current folder.
  """
  try:
    with open(summary_file, "w+", encoding="utf-8") as f:
      f.write(text)
  except Exception as e:
    logging.error(f"Failed to write to summary file {summary_file}: {e}")



def generate_junit_reports(input_dir: str, output_dir: str, summary_file: str):
  """
  Convert ZAP JSON scan reports to JUnit XML.

  Args:
    input_dir (str): Directory containing ZAP JSON reports.
    output_dir (str): Directory to write JUnit XML files.
    summary_file (str): File to write the summary.
  """

  if not os.path.isdir(input_dir):
    logging.error(f"Input directory not found: {input_dir}")
    return

  os.makedirs(output_dir, exist_ok=True)

  json_files = glob.glob(os.path.join(input_dir, "*.json"))
  if not json_files:
    logging.warning(f"No JSON files found in {input_dir}")
    return

  summary_text = ""

  for file in json_files:
    data = load_json(file)
    if data is None:
      continue

    testsuite, counters = build_testsuite(data, file)
    xml_file = os.path.join(output_dir, os.path.basename(file).replace(".json", ".xml"))

    if write_xml(xml_file, testsuite):
      summary_text += f"{xml_file} (Total: {counters['total']}, High: {counters['failures']}, Medium: {counters['skipped']}) "

  create_summary_file(summary_file, summary_text)



def parse_args():
  parser = argparse.ArgumentParser(
    description="Convert ZAP JSON reports to JUnit XML format."
  )

  parser.add_argument(
    "--input",
    required=True,
    help="Directory containing ZAP JSON reports"
  )

  parser.add_argument(
    "--output",
    required=True,
    help="Directory to write generated JUnit XML reports"
  )

  parser.add_argument(
    "--summary",
    required=True,
    help="File to write the summary"
  )

  return parser.parse_args()


def main():
  args = parse_args()
  generate_junit_reports(args.input, args.output, args.summary)


if __name__ == "__main__":
  main()
