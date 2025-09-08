FROM node:20-alpine

WORKDIR /app

# Install dependencies separately to leverage Docker layer caching
COPY package.json package-lock.json* ./
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

# Copy rest of the app (will be bind-mounted in dev)
COPY . .

EXPOSE 3000

COPY docker-entrypoint.sh ./docker-entrypoint.sh
RUN chmod +x ./docker-entrypoint.sh

ENTRYPOINT ["./docker-entrypoint.sh"]
CMD ["npm", "run", "dev"]

