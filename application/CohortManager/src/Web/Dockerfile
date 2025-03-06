FROM node:22-alpine AS base

# Install dependencies only when needed
FROM base AS deps
# Check https://github.com/nodejs/docker-node/tree/b4117f9333da4138b03a546ec926ef50a31506c3#nodealpine to understand why libc6-compat might be needed.
RUN apk add --no-cache libc6-compat
WORKDIR /app

# Copy only the specific package manager files needed
COPY package.json ./
COPY ["package-lock.json", ".npmrc", "./"]

# Remove any sensitive data from .npmrc if it exists
RUN if [ -f .npmrc ]; then \
    # Keep only the registry configurations, remove tokens or credentials
    grep -i '^registry=' .npmrc > .npmrc.tmp || true && \
    mv .npmrc.tmp .npmrc; \
    fi

# Install dependencies with npm
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts


# Rebuild the source code only when needed
FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY tsconfig.json .
COPY src/ ./src/
COPY public/ ./public/
COPY middleware/ .
COPY next.config.mjs .

# Build the application
RUN npm run build

# Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public

# Automatically leverage output traces to reduce image size
# https://nextjs.org/docs/advanced-features/output-file-tracing
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Set read-only permissions for the copied files
RUN chmod -R 544 /app/.next/standalone && \
    chmod -R 544 /app/.next/static

USER nextjs

EXPOSE 3000

ENV PORT=3000

# server.js is created by next build from the standalone output
# https://nextjs.org/docs/pages/api-reference/config/next-config-js/output
ENV HOSTNAME="0.0.0.0"
CMD ["node", "server.js"]
