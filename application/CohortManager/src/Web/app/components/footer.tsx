import Link from "next/link";

export default function Footer() {
  return (
    <footer role="contentinfo">
      <div className="nhsuk-footer-container">
        <div className="nhsuk-width-container">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h2 className="nhsuk-u-visually-hidden">Support links</h2>
              <div className="nhsuk-footer">
                <ul className="nhsuk-footer__list">
                  <li className="nhsuk-footer__list-item nhsuk-footer-default__list-item">
                    <Link
                      className="nhsuk-footer__list-item-link"
                      href="/accessibility-statement"
                    >
                      Accessibility statement
                    </Link>
                  </li>
                  <li className="nhsuk-footer__list-item nhsuk-footer-default__list-item">
                    <Link
                      className="nhsuk-footer__list-item-link"
                      href="/contact-us"
                      data-testid="contact-us-link"
                    >
                      Contact us
                    </Link>
                  </li>
                  <li className="nhsuk-footer__list-item nhsuk-footer-default__list-item">
                    <Link
                      href="/cookies-policy"
                      className="nhsuk-footer__list-item-link"
                      data-testid="cookies-link"
                    >
                      Cookies
                    </Link>
                  </li>
                  <li className="nhsuk-footer__list-item nhsuk-footer-default__list-item">
                    <Link
                      href="/terms-and-conditions"
                      className="nhsuk-footer__list-item-link"
                      data-testid="terms-and-conditions-link"
                    >
                      Terms and conditions
                    </Link>
                  </li>
                </ul>
                <div>
                  <p className="nhsuk-footer__copyright">&copy; NHS England</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
}
