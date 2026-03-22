@Library('hobom-shared-lib') _
hobomPipeline(
  serviceName:    'dev-hammer-user',
  hostPort:       '5001',
  containerPort:  '8080',
  memory:         '512m',
  cpus:           '0.5',
  envPath:        '/etc/hobom-dev/dev-hammer-user/.env',
  addHost:        true,
  submodules:     false,
  smokeCheckPath: '/health'
)
