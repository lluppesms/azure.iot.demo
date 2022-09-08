# Set up an Azure DevOps Environments

For multi-stage deployments (i.e. DEV/QA/PROD), a DevOps Environment should be set up for each stage that that is being published. On that Environment definition, the rules that are needed can set up, such as specific approvers that are needed to publish to that environment.

---
## Environment Setup

**Step 1:** Go to the project environments page and select New environment:

![Step 1](images/DevOpsEnvironment-1.jpg)

**Step 2:** Enter the name of the new environment:

![Step 2](images/DevOpsEnvironment-2.jpg)

**Step 3:** To add an approver for an environment, select that environment, click on the "More" ellipsis, and select "Approvals and checks"

![Step 3](images/DevOpsEnvironment-3.jpg)

**Step 4:** Click on the plus sign to add new checks:

![Step 4](images/DevOpsEnvironment-4.jpg)

**Step 5:** Select the "Approvals" check from the list:

![Step 5](images/DevOpsEnvironment-5.jpg)

**Step 6:** Add the authorized approvers to the list and click "Create":

![Step 6](images/DevOpsEnvironment-6.jpg)

---

The environment should be ready to use now!