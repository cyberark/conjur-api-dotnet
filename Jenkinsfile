#!/usr/bin/env groovy
@Library("product-pipelines-shared-library") _

// Automated release, promotion and dependencies
properties([
  // Include the automated release parameters for the build
  release.addParams(),
  // Dependencies of the project that should trigger builds
  dependencies([])
])

// Performs release promotion.  No other stages will be run
if (params.MODE == "PROMOTE") {
  release.promote(params.VERSION_TO_PROMOTE) { infrapool, sourceVersion, targetVersion, assetDirectory ->
    // Push the package to Nuget.org; release.promote() handles Artifactory.
    infrapool.agentSh "ASSET_DIR=${assetDirectory} summon -e promote ./release.sh"
  }
  // Copy Github Enterprise release to Github
  release.copyEnterpriseRelease(params.VERSION_TO_PROMOTE)
  return
}

pipeline {
  agent { label 'conjur-enterprise-common-agent' }

  options {
    timestamps()
    buildDiscarder(logRotator(numToKeepStr: '30'))
  }

  environment {
    // Sets the MODE to the specified or autocalculated value as appropriate
    MODE = release.canonicalizeMode()
  }

  triggers {
    cron(getDailyCronString())
    parameterizedCron(getWeeklyCronString("H(1-5)","%MODE=RELEASE"))
  }

  parameters {
    booleanParam(name: 'RUN_AZURE_TESTS', defaultValue: false, description: 'Run integration tests against Azure')
    booleanParam(name: 'RUN_GCP_TESTS', defaultValue: false, description: 'Run integration tests against GCP')
  }

  stages {
    // Aborts any builds triggered by another project that wouldn't include any changes
    stage ("Skip build if triggering job didn't create a release") {
      when {
        expression {
          MODE == "SKIP"
        }
      }
      steps {
        script {
          currentBuild.result = 'ABORTED'
          error("Aborting build because this build was triggered from upstream, but no release was built")
        }
      }
    }

    stage('Scan for internal URLs') {
      steps {
        script {
          detectInternalUrls()
        }
      }
    }

    stage('Get InfraPool ExecutorV2 Agent') {
  steps {
    script {
      INFRAPOOL_EXECUTORV2_AGENTS = getInfraPoolAgent(type: "ExecutorV2", quantity: 1, duration: 1)
      INFRAPOOL_EXECUTORV2_AGENT_0 = INFRAPOOL_EXECUTORV2_AGENTS[0]
      infrapool = infraPoolConnect(INFRAPOOL_EXECUTORV2_AGENT_0, {})

      if(params.RUN_AZURE_TESTS) {
        INFRAPOOL_AZURE_EXECUTORV2_AGENTS = getInfraPoolAgent(type: "AzureExecutorV2", quantity: 1, duration: 1)
        INFRAPOOL_AZURE_EXECUTORV2_AGENT_0 = INFRAPOOL_AZURE_EXECUTORV2_AGENTS[0]
        azureInfrapool = infraPoolConnect(INFRAPOOL_AZURE_EXECUTORV2_AGENT_0, {})
      }

      if (params.RUN_GCP_TESTS) {
        INFRAPOOL_GCP_EXECUTORV2_AGENTS = getInfraPoolAgent(type: "GcpExecutorV2", quantity: 1, duration: 1)
        INFRAPOOL_GCP_EXECUTORV2_AGENT_0 = INFRAPOOL_GCP_EXECUTORV2_AGENTS[0]
        gcpInfrapool = infraPoolConnect(INFRAPOOL_GCP_EXECUTORV2_AGENT_0, {})
      }
    }
  }
}

    stage('Validate') {
      parallel {
        stage('Changelog') {
          steps { parseChangelog(infrapool) }
        }
      }
    }

    // Generates a VERSION file based on the current build number and latest version in CHANGELOG.md
    stage('Validate Changelog and set version') {
      steps {
        script {
          updateVersion(infrapool, "CHANGELOG.md", "${BUILD_NUMBER}")
        }
      }
    }

    stage('Build and test package') {
      steps {
        script {
          catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
            infrapool.agentSh "summon -e build ./build.sh"
            infrapool.agentStash name: 'test-results', includes: '*.xml'
            unstash 'test-results'
            junit 'TestResults.xml'
            infrapool.agentArchiveArtifacts artifacts: 'bin/*', fingerprint: true
          }

          // Keep release flow running even if coverage reporting plugins fail.
          catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
            cobertura autoUpdateHealth: false,
                      autoUpdateStability: false,
                      coberturaReportFile: 'Coverage.xml',
                      conditionalCoverageTargets: '60, 0, 0',
                      failUnhealthy: true,
                      failUnstable: false,
                      lineCoverageTargets: '70, 0, 0',
                      maxNumberOfBuilds: 0,
                      methodCoverageTargets: '60, 0, 0',
                      onlyStable: false,
                      sourceEncoding: 'ASCII',
                      zoomCoverageChart: false

            codacy action: 'reportCoverage', filePath: "Coverage.xml"
          }
        }
      }
    }

    stage('Run Azure tests') {
      when {
        expression { params.RUN_AZURE_TESTS }
      }
      steps {
        script {
          azureInfrapool.agentSh '''
            set +e
            export TEST_FILTER="FullyQualifiedName~AzureAuthenticatorIntegrationTest"
            summon -e azure bash ./ci/test.sh
            rc=$?
            exit $rc
          '''
        }
      }
    }

    stage('Run GCP tests') {
      when {
        expression { params.RUN_GCP_TESTS }
      }
      environment {
        GCP_CTX_DIR = "gcp"
      }
      steps {
        script {
          // 1) Fetch token + project-id on GCP VM
          gcpInfrapool.agentSh "bash ./ci/get_gcp_token.sh host/data/test/gcp-apps/test-app conjur ${GCP_CTX_DIR}"
          gcpInfrapool.agentStash name: 'gcp-token-out', includes: "${GCP_CTX_DIR}/*"

          // 2) Bring token artifacts to regular docker-capable agent
          infrapool.agentUnstash name: 'gcp-token-out'

          // 3) Run test orchestration with GCP mode enabled
          infrapool.agentSh '''
          set -e
          export TEST_GCP=true
          export GCP_CTX_DIR=gcp
          export RUN_AZURE_TESTS=false
          export TEST_FILTER="FullyQualifiedName~GCPAuthenticatorTest"
          bash ./ci/test.sh
          '''
        }
      }
    }

    stage('Release') {
      when {
        expression {
          MODE == "RELEASE"
        }
      }
      steps {
        script {
          release(infrapool) { billOfMaterialsDirectory, assetDirectory, toolsDirectory ->
            // Copy built artifacts to assetDirectory; release() uploads them to
            // Artifactory and attaches them to the GitHub release.
            infrapool.agentSh "cp -r bin/* ${assetDirectory}"
          }
        }
      }
    }
  }

  post {
    always {
      releaseInfraPoolAgent(".infrapool/release_agents")
    }
  }
}
